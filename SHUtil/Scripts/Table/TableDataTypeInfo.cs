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
        public static XMLTableDataTypeInfo  DefaultXMLInfo  { get; } = new XMLTableDataTypeInfo  { DataRootName = "DataList", DataRowName = "Row" };
        public static JsonTableDataTypeInfo DefaultJsonInfo { get; } = new JsonTableDataTypeInfo();
    }
}
