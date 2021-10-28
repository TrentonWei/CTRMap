﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;
using System.IO;
using ESRI.ArcGIS.Geodatabase;

namespace AuxStructureLib
{
    /// <summary>
    /// 邻近图
    /// </summary>
    public class ProxiGraph
    {
        /// <summary>
        /// 点列表
        /// </summary>
        public List<ProxiNode> NodeList = null;
        /// <summary>
        /// 边列表
        /// </summary>
        public List<ProxiEdge> EdgeList = null;
        /// <summary>
        /// 父亲
        /// </summary>
        public ProxiGraph ParentGraph = null;
        /// <summary>
        /// 孩子
        /// </summary>
        public List<ProxiGraph> SubGraphs = null;
        /// <summary>
        /// 多变形的个数字段
        /// </summary>
        private int polygonCount = -1;

        /// <summary>
        /// 多边形个数属性
        /// </summary>
        public int PolygonCount
        {
            get
            {
                if (this.polygonCount != -1)
                {
                    return this.polygonCount;
                }
                else
                {
                    int count = 0;
                    if (this.NodeList == null || this.NodeList.Count == 0)
                        return -1;
                    foreach (ProxiNode node in this.NodeList)
                    {
                        if (node.FeatureType == FeatureType.PolygonType)
                            count++;
                    }
                    this.polygonCount = count;
                    return this.polygonCount;
                }
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public ProxiGraph()
        {
            NodeList = new List<ProxiNode>();
            EdgeList = new List<ProxiEdge>();
        }
        /// <summary>
        /// 创建结点列表
        /// </summary>
        /// <param name="map">地图</param>
        private void CreateNodes(SMap map)
        {
            int nID = 0;
            //点
            if (map.PointList != null)
            {
                foreach (PointObject point in map.PointList)
                {
                    ProxiNode curNode = point.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            //线
            if (map.PolylineList != null)
            {
                foreach (PolylineObject pline in map.PolylineList)
                {
                    ProxiNode curNode = pline.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            //面
            if (map.PolygonList != null)
            {
                foreach (PolygonObject polygon in map.PolygonList)
                {
                    ProxiNode curNode = polygon.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
        }


        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">骨架线</param>
        private void CreateEdges(Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {

                    curTagID = curArc.LeftMapObj.ID;
                    curType = curArc.LeftMapObj.FeatureType;
                    node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    curTagID = curArc.RightMapObj.ID;
                    curType = curArc.RightMapObj.FeatureType;
                    node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    curEdge = new ProxiEdge(curArc.ID, node1, node2);
                    this.EdgeList.Add(curEdge);
                    node1.EdgeList.Add(curEdge);
                    node2.EdgeList.Add(curEdge);
                    curEdge.NearestEdge = curArc.NearestEdge;
                    curEdge.Weight = curArc.AveDistance;
                    curEdge.Ske_Arc = curArc;
                }
            }

        }
        /// <summary>
        /// 创建结点列表
        /// </summary>
        /// <param name="map">地图</param>
        private void CreateNodesforPointandPolygon(SMap map)
        {
            int nID = 0;
            //点
            if (map.PointList != null)
            {
                foreach (PointObject point in map.PointList)
                {
                    ProxiNode curNode = point.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            ////线
            //if (map.PolylineList != null)
            //{
            //    foreach (PolylineObject pline in map.PolylineList)
            //    {
            //        ProxiNode curNode = pline.CalProxiNode();
            //        curNode.ID = nID;
            //        this.NodeList.Add(curNode);
            //        nID++;
            //    }
            //}
            //面
            if (map.PolygonList != null)
            {
                foreach (PolygonObject polygon in map.PolygonList)
                {
                    ProxiNode curNode = polygon.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
        }


        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">骨架线</param>
        private void CreateEdgesforPointandPolygon(Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType != FeatureType.PolylineType && curArc.RightMapObj.FeatureType != FeatureType.PolylineType)
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);
                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
                    }
                    //eID++;
                }
            }

        }

        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">冲突</param>
        private void CreateEdges(List<Conflict> conflicts)
        {
            if (conflicts == null || conflicts.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;
            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Conflict curConflict in conflicts)
            {
                if (curConflict.Obj1 != null && curConflict.Obj2 != null)
                {
                    if (curConflict.Obj1.ToString() == @"AuxStructureLib.SDS_PolylineObj")
                    {
                        SDS_PolylineObj curl = curConflict.Obj1 as SDS_PolylineObj;
                        curTagID = curl.ID;
                        curType = FeatureType.PolylineType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    else if (curConflict.Obj1.ToString() == @"AuxStructureLib.SDS_PolygonO")
                    {
                        SDS_PolygonO curO = curConflict.Obj1 as SDS_PolygonO;
                        curTagID = curO.ID;
                        curType = FeatureType.PolygonType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    if (curConflict.Obj2.ToString() == @"AuxStructureLib.SDS_PolylineObj")
                    {
                        SDS_PolylineObj curl = curConflict.Obj2 as SDS_PolylineObj;
                        curTagID = curl.ID;
                        curType = FeatureType.PolylineType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    else if (curConflict.Obj2.ToString() == @"AuxStructureLib.SDS_PolygonO")
                    {
                        SDS_PolygonO curO = curConflict.Obj2 as SDS_PolygonO;
                        curTagID = curO.ID;
                        curType = FeatureType.PolygonType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }

                    curEdge = new ProxiEdge(-1, node1, node2);
                    this.EdgeList.Add(curEdge);
                    node1.EdgeList.Add(curEdge);
                    node2.EdgeList.Add(curEdge);
                    //eID++;
                }
            }

        }
        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeleton(SMap map, Skeleton skeleton)
        {
            CreateNodes(map);
            CreateEdges(skeleton);
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonBuildings_Perpendicular(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            CreateNodesandPerpendicular_EdgesforPolyline_(map, skeleton);
        }


        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonBuildings(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            CreateNodesandEdgesforPolyline_LP(map, skeleton);
        }
        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonForEnrichNetwork(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            this.CreateNodesandNearestLine2PolylineVertices(map, skeleton);
            this.RemoveSuperfluousEdges();
        }

        /// <summary>
        /// 删除邻近图中多余的边
        /// </summary>
        private void RemoveSuperfluousEdges()
        {
            List<ProxiEdge> edgeList = new List<ProxiEdge>();
            foreach (ProxiEdge curEdge in this.EdgeList)
            {
                if (!this.IsContainEdge(edgeList, curEdge))
                {
                    edgeList.Add(curEdge);
                }
            }
            this.EdgeList = edgeList;
        }

        /// <summary>
        /// 是否包含该边
        /// </summary>
        /// <returns></returns>
        private bool IsContainEdge(List<ProxiEdge> edgeList,ProxiEdge edge)
        {
            if (edgeList == null || edgeList.Count == 0)
                return false;
            foreach (ProxiEdge curEdge in edgeList)
            {
                if ((edge.Node1.ID == curEdge.Node1.ID && edge.Node2.ID == curEdge.Node2.ID) || (edge.Node2.ID == curEdge.Node1.ID && edge.Node1.ID == curEdge.Node2.ID))
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandEdgesforPolyline_LP(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            bool isPerpendicular = false;

            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2Polyline(node2, curline, out isPerpendicular);


                        node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                        this.NodeList.Add(node1);
                        id++;


                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;

                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);

                        Node node = ComFunLib.MinDisPoint2Polyline(node1, curline, out isPerpendicular);

                        node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                        this.NodeList.Add(node2);
                        id++;


                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;

                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandPerpendicular_EdgesforPolyline_(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            bool isPerpendicular = false;

            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2Polyline(node2, curline, out isPerpendicular);

                        if (isPerpendicular)//仅仅加入与街道垂直的邻近边
                        {

                            node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                            this.NodeList.Add(node1);
                            id++;


                            curEdge = new ProxiEdge(curArc.ID, node1, node2);
                            this.EdgeList.Add(curEdge);
                            node1.EdgeList.Add(curEdge);
                            node2.EdgeList.Add(curEdge);

                            curEdge.NearestEdge = curArc.NearestEdge;
                            curEdge.Weight = curArc.AveDistance;
                            curEdge.Ske_Arc = curArc;
                        }
                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);

                        Node node = ComFunLib.MinDisPoint2Polyline(node1, curline, out isPerpendicular);
                        if (isPerpendicular)//仅仅加入与街道垂直的邻近边
                        {
                            node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                            this.NodeList.Add(node2);
                            id++;


                            curEdge = new ProxiEdge(curArc.ID, node1, node2);
                            this.EdgeList.Add(curEdge);
                            node1.EdgeList.Add(curEdge);
                            node2.EdgeList.Add(curEdge);

                            curEdge.NearestEdge = curArc.NearestEdge;
                            curEdge.Weight = curArc.AveDistance;
                            curEdge.Ske_Arc = curArc;
                        }
                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandNearestLine2PolylineVertices(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;

                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2PolylineVertices(node2, curline);
                        ProxiNode exitNode = GetContainNode(this.NodeList, node.X, node.Y);
                        if (exitNode == null)
                        {

                            node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                            node1.SomeValue = node.ID;
                            this.NodeList.Add(node1);
                            id++;
                        }
                        else
                        {
                            node1 = exitNode;
                        }
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
            
                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;

                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2PolylineVertices(node1, curline);
                        ProxiNode exitNode = GetContainNode(this.NodeList, node.X, node.Y);
                        if (exitNode == null)
                        {

                            node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                            node2.SomeValue = node.ID;
                            this.NodeList.Add(node2);
                            id++;
                        }
                        else
                        {
                            node2 = exitNode;
                        }
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        private ProxiNode GetContainNode(List<ProxiNode> nodeList, double x, double y)
        {

            if (nodeList == null || nodeList.Count == 0)
            {
                return null;
            }
            foreach (ProxiNode curNode in nodeList)
            {
                // int id = curNode.ID;
                ProxiNode curV = curNode;

                if (Math.Abs((1 - curV.X / x)) <= 0.000001f && Math.Abs((1 - curV.Y / y)) <= 0.000001f)
                {
                    return curV;
                }
            }
            return null;
        }


        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmConflicts(SMap map, List<Conflict> conflicts)
        {
            CreateNodes(map);
            CreateEdges(conflicts);
        }
        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyTagID(int tagID)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.TagID == tagID)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyTagIDandType(int tagID, FeatureType type)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.TagID == tagID && type == curNode.FeatureType)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyID(int ID)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.ID == ID)
                {
                    return curNode;
                }
            }
            return null;
        }
        /// <summary>
        /// 根据两端点的索引号获取边
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns></returns>
        public ProxiEdge GetEdgebyNodeIndexs(int index1, int index2)
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                if ((edge.Node1.ID == index1 && edge.Node2.ID == index2) || (edge.Node1.ID == index2 && edge.Node2.ID == index1))
                    return edge;
            }
            return null;
        }
        /// <summary>
        /// 获取所有与node相关联的边
        /// </summary>
        /// <param name="node"></param>
        /// <returns>边序列</returns>
        public List<ProxiEdge> GetEdgesbyNode(ProxiNode node)
        {
            int index = node.ID;
            List<ProxiEdge> resEdgeList = new List<ProxiEdge>();
            foreach (ProxiEdge edge in this.EdgeList)
            {
                if (edge.Node1.ID == index || edge.Node2.ID == index)
                    resEdgeList.Add(edge);
            }
            if (resEdgeList.Count > 0) return resEdgeList;
            else return null;
        }

        /// <summary>
        /// 写入SHP文件
        /// </summary>
        public void WriteProxiGraph2Shp(string filePath, string fileName, ISpatialReference pri)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            ProxiNode.Create_WriteProxiNodes2Shp(filePath, @"Node_" + fileName, this.NodeList, pri);
            ProxiEdge.Create_WriteEdge2Shp(filePath, @"Edges_" + fileName, this.EdgeList, pri);
            if (EdgeList != null && EdgeList.Count > 0)
            {
                if (this.EdgeList[0].NearestEdge != null)
                {
                    ProxiEdge.Create_WriteNearestDis2Shp(filePath, @"Nearest_" + fileName, this.EdgeList, pri);
                }
            }
        }
        /// <summary>
        /// 拷贝邻近图-   //求吸引力-2014-3-20所用
        /// </summary>
        /// <returns></returns>
        public ProxiGraph Copy()
        {
            ProxiGraph pg = new ProxiGraph();
            foreach (ProxiNode node in this.NodeList)
            {
                ProxiNode newNode = new ProxiNode(node.X, node.Y, node.ID, node.TagID, node.FeatureType);
                pg.NodeList.Add(newNode);

            }

            foreach (ProxiEdge edge in this.EdgeList)
            {
                ProxiEdge newedge = new ProxiEdge(edge.ID, this.GetNodebyID(edge.Node1.ID), this.GetNodebyID(edge.Node1.ID));
                pg.EdgeList.Add(newedge);
            }
            return pg;

        }


        /// <summary>
        /// 就算边的权重
        /// </summary>
        public void CalWeightbyNearestDistance()
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                edge.Weight = edge.NearestEdge.NearestDistance;
            }
        }
        /// <summary>
        /// 从最小外接矩形中获取相似性信息
        /// </summary>
        public void GetSimilarityInfofrmSMBR(List<SMBR> SMBRList, SMap map)
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                int tagID1 = edge.Node1.TagID;
                int tagID2 = edge.Node2.TagID;
                FeatureType type1 = edge.Node1.FeatureType;
                FeatureType type2 = edge.Node2.FeatureType;
                SMBR smbr1 = SMBR.GetSMBR(tagID1, type1, SMBRList);
                SMBR smbr2 = SMBR.GetSMBR(tagID2, type2, SMBRList);


                if (smbr1 == null || smbr2 == null)
                    continue;

                if (type1 == FeatureType.PolygonType && type2 == FeatureType.PolygonType)
                {
                    PolygonObject obj1 = PolygonObject.GetPPbyID(map.PolygonList, tagID1);
                    PolygonObject obj2 = PolygonObject.GetPPbyID(map.PolygonList, tagID2);
                    double A1 = smbr1.Direct1;
                    double A2 = smbr2.Direct1;
                    int EN1 = obj1.PointList.Count;
                    int EN2 = obj2.PointList.Count;
                    double Area1 = obj1.Area;
                    double Area2 = obj2.Area;
                    double Peri1 = obj1.Perimeter;
                    double Peri2 = obj2.Perimeter;

                    if (EN1 > EN2)
                    {
                        int temp;
                        temp = EN1;
                        EN1 = EN2;
                        EN2 = temp;
                    }
                    if (Area1 > Area2)
                    {
                        double temp;
                        temp = Area1;
                        Area1 = Area2;
                        Area2 = temp;
                    }
                    if (Peri1 > Peri2)
                    {
                        double temp;
                        temp = Peri1;
                        Peri1 = Peri2;
                        Peri2 = temp;
                    }

                    double a = Math.Abs(A1 - A2);
                    if (a > Math.PI / 2)
                    {
                        a = Math.PI - a;
                    }
                    edge.W_A_Simi = 2 * a / Math.PI;

                    edge.W_Area_Simi = Area1 / Area2;
                    edge.W_EdgeN_Simi = EN1 * 1.0 / EN2;
                    edge.W_Peri_Simi = Peri1 / Peri2;

                    edge.CalWeight();//重新计算全重
                }

                else if (type1 == FeatureType.PolylineType && type2 == FeatureType.PolylineType)
                {
                    //待续
                }
                //线线之间相似性，讨论线面之间，
                else if (type1 == FeatureType.PolygonType && type2 == FeatureType.PolylineType)
                {
                    //待续
                }

                else if (type1 == FeatureType.PolylineType && type2 == FeatureType.PolygonType)
                {
                    //待续
                }
            }
        }

        /// <summary>
        /// 用分组信息对邻近图进行优化-04-19
        /// </summary>
        /// <param name="groups"></param>
        public void OptimizeGraphbyBuildingGroups(List<GroupofMapObject> groups,SMap map)
        {
            if (groups == null || groups.Count == 0)
                return;
            foreach (GroupofMapObject curGroup in groups)
            {
                if (curGroup.ListofObjects == null || curGroup.ListofObjects.Count == 0)
                    continue;
                //获取图中对应的结点
                List<ProxiNode> curNodeList = new List<ProxiNode>();
                int tagID = curGroup.ID;
                foreach (MapObject curO in curGroup.ListofObjects)
                {
                    PolygonObject curB = curO as PolygonObject;
                    int curTagId = curB.ID;
                    FeatureType curType = curB.FeatureType;

                    ProxiNode curNode = this.GetNodebyTagIDandType(curTagId, curType);
                    curNodeList.Add(curNode);
                }

                List<ProxiEdge> curIntraEdgeList = new List<ProxiEdge>();
                List<ProxiEdge> curInterEdgeList = new List<ProxiEdge>();
                List<ProxiNode> curNeighbourNodeList = new List<ProxiNode>();//与组内邻近但在组外的结点
                List<ProxiNode> curNeighbourBoundaryNodeList = new List<ProxiNode>();
                foreach (ProxiEdge curEdge in this.EdgeList)
                {
                    ProxiNode sN = curEdge.Node1;
                    ProxiNode eN = curEdge.Node2;
                    bool f1 = this.IsContainNode(curNodeList, sN);
                    bool f2 = this.IsContainNode(curNodeList, eN);
                    if (f1 == true && f2 == true)
                    {
                        curIntraEdgeList.Add(curEdge);
                    }
                    else if (f1 == false && f2 == false)
                    {

                    }
                    else
                    {
                        curInterEdgeList.Add(curEdge);
                        if (f1 == true && f2 == false)
                        {
                            if (!this.IsContainNode(curNeighbourNodeList, eN))
                            {
                                curNeighbourNodeList.Add(eN);
                            }
                            else
                            {
                                if (eN.FeatureType == FeatureType.PolylineType)
                                    curNeighbourBoundaryNodeList.Add(eN);
                            }
                        }
                        else
                        {
                            if (!this.IsContainNode(curNeighbourNodeList, sN))
                            {
                                curNeighbourNodeList.Add(sN);
                            }
                            else
                            {
                                if (sN.FeatureType == FeatureType.PolylineType)
                                    curNeighbourBoundaryNodeList.Add(sN);
                            }
                        }
                    }

                }


                ProxiNode groupNode = AuxStructureLib.ComFunLib.CalGroupCenterPoint(curNodeList);
                groupNode.TagID = tagID;
                groupNode.FeatureType = FeatureType.Group;
                this.NodeList.Add(groupNode);//加入结点

                foreach (ProxiNode curNeighbouringNode in curNeighbourNodeList)
                {
                    if (curNeighbouringNode.FeatureType == FeatureType.PolygonType||curNeighbouringNode.FeatureType == FeatureType.PointType||curNeighbouringNode.FeatureType==FeatureType.Group)
                    {
                        ProxiEdge newEdge = new ProxiEdge(-1, groupNode, curNeighbouringNode);
                        this.EdgeList.Add(newEdge);
                    }
                    else if (curNeighbouringNode.FeatureType == FeatureType.PolylineType)
                    {
                        int curTagID = curNeighbouringNode.TagID;
                        FeatureType curType = FeatureType.PolylineType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;
                        bool isPerpendicular = true;
                        Node newNode = ComFunLib.MinDisPoint2Polyline(groupNode, curline, out isPerpendicular);
                        ProxiNode nodeonLine = new ProxiNode(newNode.X, newNode.Y, -1, curTagID, FeatureType.PolylineType);
                        this.NodeList.Add(nodeonLine);
                        ProxiEdge newEdge = new ProxiEdge(-1, groupNode, nodeonLine);
                        this.EdgeList.Add(newEdge);

                        this.NodeList.Remove(curNeighbouringNode);
                    }
                }

                foreach (ProxiEdge edge in curIntraEdgeList)
                {
                    this.EdgeList.Remove(edge);

                }
                foreach (ProxiEdge edge in curInterEdgeList)
                {
                    this.EdgeList.Remove(edge);

                }
                foreach (ProxiNode node in curNodeList)
                {
                    this.NodeList.Remove(node);
                }
                foreach (ProxiNode node in curNeighbourBoundaryNodeList)
                {
                    this.NodeList.Remove(node);
                }
                int nodeID=0;
                int edgeID=0;

                foreach (ProxiNode node in this.NodeList)
                {
                    node.ID = nodeID;
                    nodeID++;
                }
                
                 foreach (ProxiEdge edge in this.EdgeList)
                {
                    edge.ID = edgeID;
                    edgeID++;

                }
            }
        }
        /// <summary>
        /// 化简边
        /// </summary>
        /// <param name="MaxDistance"></param>
        private void SimplifyPG(double MaxDistance)
        {
            List<ProxiEdge> delEdgeList = new List<ProxiEdge>();
            foreach(ProxiEdge curEdge in this.EdgeList)
            {
                delEdgeList.Add(curEdge);
            }
            foreach (ProxiEdge delcurEdge in delEdgeList)
            {
                this.EdgeList.Remove(delcurEdge);
            }
        }
        
        /// <summary>
        /// 判断结点集合中是否含有结点-用于分组优化函数;OptimizeGraphbyBuildingGroups
        /// </summary>
        /// <param name="nodeList">结点集合</param>
        /// <param name="node">结点</param>
        /// <returns></returns>
        private bool IsContainNode(List<ProxiNode> nodeList, ProxiNode node)
        {
            if (nodeList == null || nodeList.Count == 0)
                return false;

            foreach (ProxiNode curNode in nodeList)
            {
                if (curNode.TagID==node.TagID&&curNode.FeatureType==node.FeatureType)//线上的结点
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 创建ProxiG
        /// </summary>
        /// <param name="pFeatureClass">原始图层</param>
        public void CreateProxiG(IFeatureClass pFeatureClass)
        {
            #region Create ProxiNodes
            for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
            {
                IArea pArea = pFeatureClass.GetFeature(i).Shape as IArea;
                ProxiNode CacheNode = new ProxiNode(pArea.Centroid.X, pArea.Centroid.Y, i, i);
                this.NodeList.Add(CacheNode);
            }
            #endregion

            #region Create ProxiEdges
            int edgeID = 0;
            for (int i = 0; i < pFeatureClass.FeatureCount(null)-1; i++)
            {
                for (int j = i+1; j < pFeatureClass.FeatureCount(null); j++)
                {
                    if (j != i)
                    {
                        IGeometry iGeo = pFeatureClass.GetFeature(i).Shape;
                        IGeometry jGeo = pFeatureClass.GetFeature(j).Shape;

                        IRelationalOperator iRo = iGeo as IRelationalOperator;
                        if (iRo.Touches(jGeo) || iRo.Overlaps(jGeo))
                        {
                            ProxiEdge CacheEdge = new ProxiEdge(edgeID, this.NodeList[i], this.NodeList[j]);
                            this.EdgeList.Add(CacheEdge);
                        }
                    }
                }
            }
            #endregion
        }
    }
}