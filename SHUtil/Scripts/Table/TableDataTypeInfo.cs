//////////////////////////////////////////////////////////////////////////
//
// TableDataTypeInfo
// 
// Created by Shoori.
//
// Copyright 2024-2025 SongMyeongWon.
// All rights reserved
//
//////////////////////////////////////////////////////////////////////////
// Version 1.0
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SHUtil.Table
{
    public interface ITableDataTypeInfo
    {
    }

    public class XMLTableDataTypeInfo : ITableDataTypeInfo
    {
        public string DataRootName;
        public string DataRowName;
    }

    public class JsonTableDataTypeInfo : ITableDataTypeInfo
    {
    }

    public static class TableDataTypeInfoUtil
    {
        public static XMLTableDataTypeInfo DefaultXMLInfo { get; private set; }
        public static JsonTableDataTypeInfo DefaultJsonInfo { get; private set; }

        static TableDataTypeInfoUtil()
        {
            DefaultXMLInfo = new XMLTableDataTypeInfo();
            DefaultXMLInfo.DataRowName = "Row";
            DefaultXMLInfo.DataRootName = "DataList";

            DefaultJsonInfo = new JsonTableDataTypeInfo();
        }
    }
}
