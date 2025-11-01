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

namespace SHUtil.Table
{
    public abstract class TableInfoBase
    {
        public int IDN { get; private set; }
        public string ID { get; private set; }

        public virtual bool IsValid
        {
            get
            {
                return true;
            }
        }

        //----------------------------------------------------------------------------------
        public abstract void Load(XmlSelector node);

        //----------------------------------------------------------------------------------
        public abstract void LoadAppend(XmlSelector node);

        //----------------------------------------------------------------------------------
        public virtual string DebugInfo()
        {
            return "";
        }

        //----------------------------------------------------------------------------------
        public void Init(int idn, string id)
        {
            IDN = idn;
            ID = id;
        }

        public const string DEFAULT_PATH_ROOT = "Table";
        public const string EXPORT_PATH = "Export";
        public const string EXPORT_PATH_CLIENT = "ClientXML";
        public const string EXPORT_PATH_XML = "XML";
        public const string EXPORT_PATH_ENCRYPT = "Encrypt";
        public const string EXPORT_PATH_BINARY = "Binary";
        public const string EXPORT_PATH_BINARY_ENCRYPT = "BinaryEncrypt";
        public const string EXTENSION_XML = ".xml";
        public const string EXTENSION_XML_ENCRYPT = ".xenc";
        public const string EXTENSION_XML_BINARY = ".xbin";
        public const string EXTENSION_XML_BINARY_ENCRYPT = ".xbinenc";
    }
}
