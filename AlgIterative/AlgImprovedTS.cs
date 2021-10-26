﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using System.Data;
using AuxStructureLib.IO;

namespace AlgIterative
{

    /// <summary>
    /// 改进的禁忌搜索算法
    /// </summary>
    public class AlgImprovedTS
    {
        /// <summary>
        /// 状态
        /// </summary>
        public State State = null;
        //简单数据结构数据
        public SDS MapSDS = null;
        //各类冲突权值
        public double PLCost = 10;//面线冲突权值
        public double PPCost = 5;//面面冲突权值
        public double DispCost = 1;//位置精度冲突权值
        public double DTol = 5;//移位范围，最大移位距离
        public double PPTol = 4;//面面之间距离阈值
        public double PLTol = 8;//面线之间距离阈值
        public DispVectorTemplate DispTemplate = null;//移位模版
        ConflictDetection ConflictDetector = null;//冲突探测对象

        public List<Move> Move_tabu_list = null;//移动位置禁忌表
        public List<State> State_tabu_list = null;//地图状态禁忌表
        public List<Move> Candidate_list = null;//候选表

        public int C = 10;
        //C1 C2、 均为待定系，需要根据经验设置Move_tabu_listd的大小 Size=C1+C2*f
        public int C1 = 20;
        public int C2 = 1;
        public int I = 100;//迭代次数

        VoronoiDiagram VD=null; //V图
        SMap OMap=null;//原始地图
        SMap SMap = null;//SMap对象，保存当前状态
        SMap SMap1 = null;//SMap对象，保存当前状态，用于生V图

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="plcost">线面冲突权值</param>
        /// <param name="ppcost">面面冲突权值</param>
        /// <param name="dcost">移位权重</param>
        /// <param name="dtol">移位距离阈值</param>
        /// <param name="pltol">面线距离阈值</param>
        /// <param name="pptol">面面距离阈值</param>
        /// <param name="c1">待定系数1</param>
        /// <param name="c2">待定系数2</param>
        /// <param name="c">待定系数</param>
        /// <param name="i">迭代次数</param>
        /// <param name="vd">V图</param>
        /// <param name="omap">原始地图对象</param>
        /// <param name="smap">地图对象</param>
        /// <param name="smap1">加密后的地图对象，用于生成V图</param>
        public AlgImprovedTS(SDS map, 
            double plcost, 
            double ppcost, 
            double dcost, 
            double dtol, 
            double pltol, 
            double pptol,
            int c1, 
            int c2, 
            int c, 
            int i,
            VoronoiDiagram vd,
            SMap omap,
            SMap smap,
            SMap smap1
        )
        {
            PLCost = plcost;
            PPCost = ppcost;
            DispCost = dcost;
            VD = vd;
            OMap = omap;
            SMap = smap;//SMap对象，保存当前状态
            SMap1 = smap1;//SMap对象，保存当前状态，用于生V图
            DTol =dtol ;
            PLTol = pltol;
            PPTol = pptol;

            State = new State();

            InitDispTemplate();
            this.DispTemplate.CreateIsTriableList1(VD, OMap, dtol);

            MapSDS = map;

            ConflictDetector = new ConflictDetection(MapSDS);

            C = c;
            C1 = c1;
            C2 = c2;
            I = i;

            Move_tabu_list = new List<Move>();
            State_tabu_list = new List<State>();
        }
            /// <summary>
            /// 创建候选表-按冲突排序
            /// </summary>
            /// <param name="state">状态</param>
            /// <returns>候选表</returns>
            private List<Move> GenerateCandidateList(State state)
            {
                List<Move> candidate_list = new List<Move>();
                Move newMove = null;

                SDS_PolygonO Move_Obj = null;
                int curPosID = -1;
                int n = state.TrialPositionIDs.Length;
              
                for (int i = 0; i < n; i++)
                {
                 
                    Move_Obj = this.MapSDS.PolygonOObjs[i];
                    //获得对象的可试探位置
                    int m = Move_Obj.TrialPosList.Count;
                    curPosID = state.TrialPositionIDs[i];
                    for (int j = 0; j < m; j++)
                    {
                        if (curPosID != j)
                        {
                            List<Conflict> oNewC = null;//对象新的冲突
                            List<Conflict> oCurC = null;//对象当前冲突
                            int id;
                            double dx;
                            double dy;
                            double dx0;
                            double dy0;
                            double b = this.Evaluate_NewState(state, Move_Obj, j, out oCurC, out oNewC, out id, out dx, out dy, out dx0, out dy0);
                            newMove = new Move(i, j, b);
                            candidate_list.Add(newMove);
                        }
                    }
                }
                Move.SortMoves(candidate_list);
                return candidate_list;
            }

            /// <summary>
            /// 判断是否存在三角形的穿越
            /// </summary>
            /// <param name="Polygon">多边i型你</param>
            /// <returns></returns>
            /// 
            private bool HasTriangleInverted(SDS_PolygonO Polygon)
            {
                List<SDS_Triangle> triList = Polygon.GetSurroundingTris();
                foreach (SDS_Triangle curTri in triList)
                {
                    if (!curTri.IsAnticlockwise())
                        return true;
                }
                return false;
            }
            /// <summary>
            /// 通过更改一个位置计算新的目标值
            /// 思路：先将CurState中与Obj相关的所有冲突揪出来，然后重新计算Obj的冲突
            /// 两者对比评价值，他们的差别即是两个不同状态的差别
            /// </summary>
            /// <param name="CurState">当前状态</param>
            /// <param name="Obj">当前假设要移位的对象</param
            /// <param name="Trial_PositionNO">当前试探的新位置</param>
            /// <param name="ObjCurConflicts">对象当前涉及的冲突（输出）</param>
            /// <param name="ObjNewConflicts">假设对象移位后涉及的冲突（输出）</param>
            /// <param name="index">对象的索引号（在所有对象中的排列号）（输出）</param>
            /// <param name="dx">移位X（输出）</param>
            /// <param name="dy">移位Y（输出）</param>
            /// <returns>新的评价值</returns>
            private double Evaluate_NewState(State CurState, SDS_PolygonO Obj, int Trial_PositionNO,
                out List<Conflict> ObjCurConflicts, out List<Conflict> ObjNewConflicts,
                out int index, out double dx, out double dy, out double dx0, out double dy0)
            {

                index = 0;
                dy0 = 0;
                dx0 = 0;
                ObjCurConflicts = null;
                ObjNewConflicts = null;
                //找到对象在数组中的位置
                for (index = 0; index < this.MapSDS.PolygonOObjs.Count; index++)
                {
                    if (this.MapSDS.PolygonOObjs[index] == Obj)
                        break;
                }

                if (CurState.TrialPositionIDs[index] != 0)
                {
                    dx0 = Obj.TrialPosList[CurState.TrialPositionIDs[index]].Dx;
                    dy0 = Obj.TrialPosList[CurState.TrialPositionIDs[index]].Dy;
                    Obj.Translate((-1.0) * dx0, (-1.0) * dy0);
                }

                dx = Obj.TrialPosList[Trial_PositionNO].Dx;
                dy = Obj.TrialPosList[Trial_PositionNO].Dy;
                Obj.Translate(dx, dy);

                //已经用了V图不需要以下判断了
                ////首先判断是否存在三角形的穿越
                //if (this.HasTriangleInverted(Obj))
                //{
                //    Obj.Translate((-1.0) * dx, (-1.0) * dy);
                //    Obj.Translate(dx0, dy0);
                //    return double.PositiveInfinity;
                //}

                ObjNewConflicts = this.ConflictDetector.ObjectConflictDetectPP_PL(Obj,this.PPTol,this.PLTol);
                double oNewCost = CostFunction(Obj,ObjNewConflicts, Trial_PositionNO);
                ObjCurConflicts = CurState.GetObjectConflict(Obj);
                double oCurCost = CostFunction(Obj, ObjCurConflicts, CurState.TrialPositionIDs[index]);
                double NewCost = CurState.State_Cost + (oNewCost - oCurCost);
                
                Obj.Translate((-1.0) * dx, (-1.0) * dy);//还原
                Obj.Translate(dx0, dy0);//还原

                return NewCost;
            }

            /// <summary>
            /// 更新状态
            /// 思路：先将CurState中与Obj相关的所有冲突揪出来，然后重新计算Obj的冲突
            /// 两者对比评价值，他们的差别即是两个不同状态的差别
            /// </summary>
            /// <param name="CurState">当前状态</param>
            /// <param name="Obj">当前假设要移位的对象</param
            /// <param name="Trial_PositionNO">当前试探的新位置</param>
            /// <param name="ObjCurConflicts">对象当前涉及的冲突（输出）</param>
            /// <param name="ObjNewConflicts">假设对象移位后涉及的冲突（输出）</param>
            /// <param name="index">对象的索引号（在所有对象中的排列号）（输出）</param>
            /// <param name="dx">移位X（输出）</param>
            /// <param name="dy">移位Y（输出）</param>
            /// <returns>新的评价值</returns>
            private void Update_NewState(State CurState, int ObjID, int Trial_PositionNO)
            {

                double dy0 = 0;
                double dx0 = 0;
                List<Conflict> ObjCurConflicts = null;
                List<Conflict> ObjNewConflicts = null;
                SDS_PolygonO Obj = this.MapSDS.PolygonOObjs[ObjID];
                PolygonObject sobj = this.SMap.PolygonList[ObjID];
                PolygonObject sobj1 = this.SMap1.PolygonList[ObjID];

                if (CurState.TrialPositionIDs[ObjID] != 0)
                {
                    dx0 = Obj.TrialPosList[CurState.TrialPositionIDs[ObjID]].Dx;
                    dy0 = Obj.TrialPosList[CurState.TrialPositionIDs[ObjID]].Dy;
                    Obj.Translate((-1.0) * dx0, (-1.0) * dy0);
                    sobj.Translate((-1.0) * dx0, (-1.0) * dy0);
                    sobj1.Translate((-1.0) * dx0, (-1.0) * dy0);
                }

                double dx = Obj.TrialPosList[Trial_PositionNO].Dx;
                double dy = Obj.TrialPosList[Trial_PositionNO].Dy;
                Obj.Translate(dx, dy);
                sobj.Translate(dx, dy);
                sobj1.Translate(dx, dy);

                ObjNewConflicts = this.ConflictDetector.ObjectConflictDetectPP_PL(Obj, this.PPTol,this.PLTol);
                double oNewCost = CostFunction(Obj,ObjNewConflicts, Trial_PositionNO);
                ObjCurConflicts = CurState.GetObjectConflict(Obj);
                double oCurCost = CostFunction(Obj,ObjCurConflicts, CurState.TrialPositionIDs[ObjID]);
                double NewCost = CurState.State_Cost + (oNewCost - oCurCost);


                CurState.TrialPositionIDs[ObjID] = Trial_PositionNO;//???更新状态码Trial_PositionNO

                if (ObjCurConflicts != null)
                {
                    foreach (Conflict c in ObjCurConflicts)
                    {
                        CurState.Conflicts.Remove(c);
                    }
                }
                if (ObjNewConflicts != null)
                {
                    CurState.Conflicts.AddRange(ObjNewConflicts);
                }
                CurState.State_Cost = NewCost;

                return;
            }


            /// <summary>
            /// 初始化移位向量模版-16方向
            /// </summary>
            private void InitDispTemplate()
            {
                List<TrialPosiDisGroup> TPDGList = new List<TrialPosiDisGroup>();
                TrialPosiDisGroup g1 = new TrialPosiDisGroup(DTol / 4.0, 0, Math.PI / 2);
                TrialPosiDisGroup g2 = new TrialPosiDisGroup(DTol / 2.0, Math.PI / 8, Math.PI / 4);
                TrialPosiDisGroup g3 = new TrialPosiDisGroup(3.0 * DTol / 4.0, 0, Math.PI / 8);
                TrialPosiDisGroup g4 = new TrialPosiDisGroup(DTol, 0, Math.PI / 16);

                TPDGList.Add(g1);
                TPDGList.Add(g2);
                TPDGList.Add(g3);
                TPDGList.Add(g4);
                DispTemplate = new DispVectorTemplate(TPDGList);
                DispTemplate.CalTriPosition();
            }

            /// <summary>
            /// 计算初始目标值
            /// 思路：找出所有冲突，然后移位
            /// </summary>
            /// <returns></returns>
            public double Evaluate_InitState()
            {
                List<Conflict> conflictList = ConflictDetector.ConflictDetect(this.PLTol,this.PPTol);
                State.Conflicts = conflictList;
                int n = MapSDS.PolygonOObjs.Count;
                State.TrialPositionIDs = new int[n];
                for (int i = 0; i < n; i++)
                {
                    State.TrialPositionIDs[i] = 0;
                }
                this.State.State_Cost = CostFunction(State);
                return this.State.State_Cost;
            }

            /// <summary>
            /// 计算总状态值
            /// </summary>
            /// <returns>状态值</returns>
            private double CostFunction(State state)
            {
                double cost = 0;

                foreach (Conflict conflict in state.Conflicts)
                {
                    if (conflict.ConflictType == @"PP")
                    {

                        cost += (this.PPCost * (this.PPTol - conflict.Distance));
                    }
                    else if (conflict.ConflictType == @"PL")
                    {

                        cost += (this.PLCost * (this.PLTol - conflict.Distance));

                    }
                }
                for (int i = 0; i < state.TrialPositionIDs.Length; i++)
                {

                    cost += this.DispCost * this.MapSDS.PolygonOObjs[i].TrialPosList[state.TrialPositionIDs[i]].cost;
                }

                return cost;
            }

            /// <summary>
            /// 计算单个对象的状态值
            /// </summary>
            /// <returns>状态值</returns>
            private double CostFunction(ISDS_MapObject Obj, List<Conflict> Conflicts, int PosIndex)
            {
                double cost = 0;

                if (Conflicts != null && Conflicts.Count > 0)
                {
                    foreach (Conflict conflict in Conflicts)
                    {
                        if (conflict.ConflictType == @"PP")
                        {

                            cost += (this.PPCost * (this.PPTol - conflict.Distance));
                        }
                        else if (conflict.ConflictType == @"PL")
                        {

                            cost += (this.PLCost * (this.PLTol - conflict.Distance));
                        }
                    }
                }
                double d = (Obj as SDS_PolygonO).TrialPosList[PosIndex].cost;
                cost += this.DispCost * d;
                return cost;
            }

            /// <summary>
            /// 执行禁忌搜索
            /// </summary>
            public void DoTS()
            {
                double a = this.Evaluate_InitState();
                int i = 0;
                int j = 0;
                int moveTabuSize = -1;

                State cur_State = this.State;

                if (a == 0)//或小于摸个阈值
                    return;
                else
                {
                    Move newMove = null;
                    State newState = null;
                    this.Candidate_list = GenerateCandidateList(cur_State);
                    while (a > 0 && i <= I)
                    {
                        foreach (Move curMove in this.Candidate_list)
                        {
                            newState = cur_State.ChangeNewState(curMove);
                            if (this.IsMove_Tabued(curMove, moveTabuSize) || this.IsState_Tabued(newState))
                            {//如果被禁忌了
                                if (curMove.Cost < a)
                                {//但目标函数比当前值小，则接受
                                    newMove = curMove;
                                    break;
                                }
                            }
                            else
                            {
                                newMove = curMove;
                                break;
                            }
                        }
                        if (newMove == null)
                        {
                            newMove = this.Move_tabu_list[this.Move_tabu_list.Count - moveTabuSize];
                            newMove.GetMove(this.Candidate_list);
                            newState = cur_State.ChangeNewState(newMove);
                        }

                        if (j < this.C)
                        {
                            j++;
                        }
                        else
                        {
                            j = 0;
                            //更新MOVE禁忌表的长度
                            moveTabuSize = (int)(C1 + C2 * cur_State.State_Cost);
                        }

                        this.Move_tabu_list.Add(newMove);
                        this.State_tabu_list.Add(newState);

                        if (newMove.Cost < a)
                        {
                            i = 0;
                            this.Update_NewState(cur_State, newMove.move_Obj, newMove.move_Pos);
                            a = cur_State.State_Cost;
                            this.Candidate_list = GenerateCandidateList(cur_State);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }


            /// <summary>
            /// 执行禁忌搜索
            /// </summary>
            /// <param name="curPath">当前存储路径</param>
            /// <param name="Init_No">冲突个数</param>
            /// <param name="Init_Cost">冲突值</param>
            public void DoTS(string curPath, out int Init_No, out double Init_Cost)
            {
                //用于记录冲突信息的表格
                int count = 1;

                #region 创建表格
                DataTable dtConflict = new DataTable("Time");
                dtConflict.Columns.Add("MoveID", typeof(int));
                dtConflict.Columns.Add("Conf_No", typeof(int));
                dtConflict.Columns.Add("Conf_Cost", typeof(double));
                #endregion

                double a = this.Evaluate_InitState();
                Init_Cost = a;//输出初始冲突值
                Init_No = this.State.Conflicts.Count;//输出初始冲突个数
                
                #region 添加初始冲突信息记录
                DataRow dr = dtConflict.NewRow();
                dr[0] = count;
                dr[1] = Init_No;
                dr[2] = Init_Cost;
                dtConflict.Rows.Add(dr);
                #endregion

                int i = 0;
                int j = 0;
                int moveTabuSize = -1;

                State cur_State = this.State;

                if (a == 0)//或小于摸个阈值
                    return;
                else
                {
                    Move newMove = null;
                    State newState = null;
                    this.Candidate_list = GenerateCandidateList(cur_State);
                    while (a > 0 && i <= I)
                    {
                        foreach (Move curMove in this.Candidate_list)
                        {
                            newState = cur_State.ChangeNewState(curMove);
                            if (this.IsMove_Tabued(curMove, moveTabuSize) || this.IsState_Tabued(newState))
                            {//如果被禁忌了
                                if (curMove.Cost < a)
                                {//但目标函数比当前值小，则接受
                                    newMove = curMove;
                                    break;
                                }
                            }
                            else
                            {
                                newMove = curMove;
                                break;
                            }
                        }
                        if (newMove == null)
                        {
                            newMove = this.Move_tabu_list[this.Move_tabu_list.Count - moveTabuSize];
                            newMove.GetMove(this.Candidate_list);
                            newState = cur_State.ChangeNewState(newMove);
                        }

                        if (j < this.C)
                        {
                            j++;
                        }
                        else
                        {
                            j = 0;
                            //更新MOVE禁忌表的长度
                            moveTabuSize = (int)(C1 + C2 * cur_State.State_Cost);
                        }

                        this.Move_tabu_list.Add(newMove);
                        this.State_tabu_list.Add(newState);

                        if (newMove.Cost < a)
                        {
                            count++;

                            i = 0;
                            this.Update_NewState(cur_State, newMove.move_Obj, newMove.move_Pos);
                            a = cur_State.State_Cost;
                            this.Candidate_list = GenerateCandidateList(cur_State);

                            #region 添加冲突信息记录
                            dr = dtConflict.NewRow();
                            dr[0] = count;
                            dr[1] = this.State.Conflicts.Count;
                            dr[2] = this.State.State_Cost;
                            dtConflict.Rows.Add(dr);
                            #endregion
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
                //输出冲突信息表
                TXTHelper.ExportToTxt(dtConflict, curPath + @"\Conflicit.txt");
            }
            /// <summary>
            /// 判断移动是否被禁忌
            /// </summary>
            /// <param name="move"></param>
            /// <returns></returns>
            private bool IsMove_Tabued(Move move, int Size)
            {
                int n = this.Move_tabu_list.Count;
                if (n >= Size)
                {
                    for (int i = n - 1; i >= n - Size; i--)
                    {
                        if (move.move_Obj == this.Move_tabu_list[i].move_Obj && move.move_Pos == this.Move_tabu_list[i].move_Pos)
                            return true;
                    }
                }
                else
                {
                    for (int i = n - 1; i >= 0; i--)
                    {
                        if (move.move_Obj == this.Move_tabu_list[i].move_Obj && move.move_Pos == this.Move_tabu_list[i].move_Pos)
                            return true;
                    }
                }
                return false;
            }
            /// <summary>
            /// 状态是否被禁忌
            /// </summary>
            /// <param name="state"></param>
            /// <returns></returns>
            private bool IsState_Tabued(State state)
            {
                int n = state.TrialPositionIDs.Length;
                foreach (State curState in this.State_tabu_list)
                {
                    if (curState.IsEqual(state))
                        return true;
                }
                return false;
            }
        
    }
}
