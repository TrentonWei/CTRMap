﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Genetic;
using AForge;
using AForge.Controls;
using AuxStructureLib;

namespace AlgIterative.AlgGA
{
    /// <summary>
    /// 适应度函数
    /// </summary>
    public class  MapFitnessFunction:IFitnessFunction
    {
        public  SDS Map = null;
        public DispVectorTemplate DisVTemp = null;
        ConflictDetection ConflictDetector = null;
        public   double PLDtol= 0;
        public double PPDtol = 0;
        public double Dstancetol = 0;
        public double PLCost = 10;
        public double PPCost = 5;
        public double DispCost = 1;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        public MapFitnessFunction(SDS map, 
            DispVectorTemplate disVTemp,
            ConflictDetection conflictDetector,
            double PLDtol,
            double PPDtol,
            double dstancetol,
            double pLCost,
            double pPCost,
            double dispCost)
        {
            this.Map = map;
            DisVTemp = disVTemp;
            ConflictDetector = conflictDetector;
            this.PLDtol = PLDtol;
            this.PPDtol = PPDtol;
            Dstancetol = dstancetol;
            PLCost = pLCost;
            PPCost = pPCost;
            DispCost = dispCost;
        }

        #region IFitnessFunction 成员
        /// <summary>
        /// 计算chromosome的适应值
        /// </summary>
        /// <param name="chromosome">1个个体</param>
        /// <returns>适应值</returns>
        public double Evaluate(IChromosome chromosome)
        {
            return 1 / (ConflictCost(chromosome) + 1);
        }
        #endregion

        /// <summary>
        /// 计算冲突评价值
        /// </summary>
        /// <param name="chromosome">1个个体</param>
        /// <returns>适应值</returns>
        public double ConflictCost(IChromosome chromosome)
        {
            double conflictCost=0;
            ushort[] state = ((MapChromosome)chromosome).Value;

            // check path size
            if (state.Length != this.Map.PolygonOObjs.Count)
            {
                throw new ArgumentException("错误的地图配置，目标个数不匹配！");
            }

            MoveObjects(state);
            //if (HasTriangleInverted(state))//如果存在拓扑冲突则冲突值设为正无穷。
               // return double.PositiveInfinity;
            List<Conflict> clist = ConflictDetector.ConflictDetect(PLDtol,PPDtol);
            ((MapChromosome)chromosome).ConflictsNo = clist.Count;
            conflictCost = CostFunction(clist, state);
            ((MapChromosome)chromosome).ConflictsCost = conflictCost;
            
            ReturnObject(state);

            return conflictCost;
        }
        /// <summary>
        /// 移动到一个新的状态
        /// </summary>
        /// <param name="state"></param>
        public void MoveObjects(ushort[] state)
        {
            int n = state.Length;
            TrialPosition curPos = null;
            double curX = -1;
            double curY = -1; 
            for (int i = 0; i < n; i++)
            {
                if (state[i] != 0)
                {
                    curPos = this.DisVTemp.TrialPosiList[state[i]];
                    curX = curPos.Dx;
                    curY = curPos.Dy;
                    this.Map.PolygonOObjs[i].Translate(curX, curY);
                }
            }
        }
        /// <summary>
        /// 返回原来的位置
        /// </summary>
        /// <param name="state"></param>
        private void ReturnObject(ushort[] state)
        {
            int n = state.Length;
            TrialPosition curPos = null;
            double curX = -1;
            double curY = -1;
            for (int i = 0; i < n; i++)
            {
                if (state[i] != 0)
                {
                    curPos = this.DisVTemp.TrialPosiList[state[i]];
                    curX = (-1) * curPos.Dx;
                    curY = (-1) * curPos.Dy;
                    this.Map.PolygonOObjs[i].Translate(curX, curY);
                }
            }
        }
        /// <summary>
        /// 检查是否有三角形穿越现象(拓扑检查)
        /// </summary>
        /// <returns>是否有拓扑错误</returns>
        private bool HasTriangleInverted(ushort[] state)
        {
            int n=state.Length;
            for (int i = 0; i < n; i++)
            {
                if (state[i] !=0)
                {
                    ISDS_MapObject Obj = this.Map.PolygonOObjs[i];
                    if (ConflictDetector.HasTriangleInverted(Obj as SDS_PolygonO))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算总状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(List<Conflict> Conflicts, ushort[] state)
        {
            double cost = 0;

            foreach (Conflict conflict in Conflicts)
            {
                if (conflict.ConflictType == @"PP")
                {
                    //cost += this.PPCost;
                    cost += (this.PPCost * (this.PPDtol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {

                    //cost += this.PLCost;
                    cost += (this.PLCost * (this.PLDtol - conflict.Distance));

                }
            }
            for (int i = 0; i < state.Length; i++)
            {
                if (state[i] !=0)
                {
                    cost += this.DispCost * this.DisVTemp.TrialPosiList[state[i]].Distance;
                    // cost += 1;
                }
            }

            return cost;
        }

    }
}