//////////////////////////////////////////////////////////////////////////
//
// XmlSelector
// 
// Created by Shoori.
//
// Copyright 2025 SongMyeongWon.
// All rights reserved
//
//////////////////////////////////////////////////////////////////////////
// Version 1.0
//
//////////////////////////////////////////////////////////////////////////

using System.Xml;

namespace SHUtil
{
    public class XmlSelector
    {
        public XmlNode m_XMLNode;
        public XmlReader m_XMLReader;
        public XmlBinary m_XMLBinary;
        public XmlBinary.Node m_XMLBinaryCurrNode;

        public int AttributeCount
        {
            get
            {
                if (m_XMLNode != null)
                    return m_XMLNode.Attributes.Count;

                if (m_XMLReader != null)
                    return m_XMLReader.AttributeCount;

                if (m_XMLBinaryCurrNode != null)
                    return m_XMLBinaryCurrNode.AttributeCount;

                return 0;
            }
        }

        //----------------------------------------------------------------------------------
        public XmlNode SelectSingleNode(string node_name)
        {
            if (m_XMLNode != null)
                return m_XMLNode.SelectSingleNode(node_name);

            return null;
        }

        //----------------------------------------------------------------------------------
        public bool HasAttribute(string attr_name)
        {
            if (m_XMLNode != null)
                return m_XMLNode.Attributes[attr_name] != null;

            if (m_XMLReader != null)
                return m_XMLReader.GetAttribute(attr_name) != null;

            if (m_XMLBinaryCurrNode != null)
                return m_XMLBinaryCurrNode.HasAttribute(attr_name);

            return false;
        }
    }
}
