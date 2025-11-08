//////////////////////////////////////////////////////////////////////////
//
// TableInfoBase
// 
// Created by Shoori.
//
// Copyright 2024 SongMyeongWon.
// All rights reserved
//
//////////////////////////////////////////////////////////////////////////
// Version 1.0
//
//////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Xml;

namespace SHUtil.Table
{
    //----------------------------------------------------------------------------------
    public abstract class TableInfoJsonBase : TableInfoBase
    {
        //----------------------------------------------------------------------------------
        public override void Load(object data)
        {
            var xmlNode = data as Dictionary<string, string>;
            if (xmlNode != null)
                Load(xmlNode);
        }

        //----------------------------------------------------------------------------------
        public override void LoadAppend(object appendData)
        {
            var xmlNode = appendData as Dictionary<string, string>;
            if (xmlNode != null)
                LoadAppend(xmlNode);
        }

        //----------------------------------------------------------------------------------
        protected abstract void Load(Dictionary<string, string> nodeData);
        //----------------------------------------------------------------------------------
        protected abstract void LoadAppend(Dictionary<string, string> nodeData);
    }

    //----------------------------------------------------------------------------------
    public abstract class TableInfoXmlBase : TableInfoBase
    {
        //----------------------------------------------------------------------------------
        public override void Load(object data)
        {
            var xmlNode = data as XmlNode;
            if (xmlNode != null)
                Load(xmlNode);
        }

        //----------------------------------------------------------------------------------
        public override void LoadAppend(object appendData)
        {
            var xmlNode = appendData as XmlNode;
            if (xmlNode != null)
                LoadAppend(xmlNode);
        }

        //----------------------------------------------------------------------------------
        protected abstract void Load(XmlNode nodeData);
        //----------------------------------------------------------------------------------
        protected abstract void LoadAppend(XmlNode nodeData);
    }

    //----------------------------------------------------------------------------------
    public abstract class TableInfoBase
    {
        public int Idx { get; private set; }
        public int Idn { get; private set; }
        public string Id { get; private set; }

        //----------------------------------------------------------------------------------
        public void InitByIdn(int idx, int idn)
        {
            Idx = idx;
            Idn = idn;
        }

        //----------------------------------------------------------------------------------
        public void InitById(int idx, string id)
        {
            Idx = idx;
            Id = id;
        }

        //----------------------------------------------------------------------------------
        public abstract void Load(object data);

        //----------------------------------------------------------------------------------
        public abstract void LoadAppend(object appendData);
    }
}
