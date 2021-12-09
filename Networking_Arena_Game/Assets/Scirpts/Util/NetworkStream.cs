using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

public class NetworkStream
{
    // --- Input --- 
    MemoryStream idData = new MemoryStream();
    MemoryStream fncData = new MemoryStream();
    MemoryStream objData = new MemoryStream();

    int fncCount = 0;
    int objCount = 0;

    readonly static int INT_SIZE = sizeof(int);
    readonly static int DOUBLE_SIZE = sizeof(double);
    public enum Keyword
    {
        NULL = -1,
        OBJECT = 0,
        FUNCTION = 1,
        FNC_NEW = 2,
        FNC_NEW_REPLY = 3,
        FNC_DESTROY = 4,
        OBJ_BULLET = 5,
        OBJ_GRENADE = 6
    }

    public void AddIdData(int id)
    {
        idData.Write(BitConverter.GetBytes(id), 0, INT_SIZE);
    }

    public void AddNewFunction(int netId, Keyword objType)
    {
        fncData.Write(BitConverter.GetBytes((int)Keyword.FNC_NEW), 0, INT_SIZE);
        fncData.Write(BitConverter.GetBytes(netId), 0, INT_SIZE);
        fncData.Write(BitConverter.GetBytes((int)objType), 0, INT_SIZE);

        ++fncCount;
    }

    public void AddDestroyFunction(int netId)
    {
        fncData.Write(BitConverter.GetBytes((int)Keyword.FNC_DESTROY), 0, INT_SIZE);
        fncData.Write(BitConverter.GetBytes(netId), 0, INT_SIZE);

        ++fncCount;
    }

    public void AddObject(int netId, Vector3 position, Vector3 velocity)
    {
        objData.Write(BitConverter.GetBytes(netId), 0, INT_SIZE);

        objData.Write(BitConverter.GetBytes((double)position.x), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)position.y), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)position.z), 0, DOUBLE_SIZE);

        objData.Write(BitConverter.GetBytes((double)velocity.x), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)velocity.y), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)velocity.z), 0, DOUBLE_SIZE);

        ++objCount;
    }

    public byte[] GetBuffer(bool dispose = true)
    {
        MemoryStream data = new MemoryStream();

        byte[] buffer = idData.GetBuffer();
        data.Write(buffer, 0, (int)idData.Length);

        buffer = fncData.GetBuffer();
        if (buffer.Length > 0)
        {
            data.Write(BitConverter.GetBytes((int)Keyword.FUNCTION), 0, INT_SIZE);
            data.Write(BitConverter.GetBytes(fncCount), 0, INT_SIZE);
            data.Write(buffer, 0, (int)fncData.Length);
        }

        buffer = objData.GetBuffer();
        if (buffer.Length > 0)
        {
            data.Write(BitConverter.GetBytes((int)Keyword.OBJECT), 0, INT_SIZE);
            data.Write(BitConverter.GetBytes(objCount), 0, INT_SIZE);
            data.Write(buffer, 0, (int)objData.Length);
        }

        if (dispose)
        {
            idData.Dispose();
            fncData.Dispose();
            objData.Dispose();
        }
        data.Write(Encoding.UTF8.GetBytes("XXXX"), 0, "XXXX".Length);

        byte[] outputData = data.GetBuffer();
        outputData = ByteArray.TrimEnd(outputData);
        return outputData;
    }
    // --- !Input --- 

    // --- Output --- 
    public struct Function
    {
        public Function(Keyword functionType, int netId, Keyword objType)
        {
            this.functionType = functionType;
            this.netId = netId;
            this.objType = objType;
        }

        public Keyword functionType;
        public int netId;
        public Keyword objType;
    }
    public struct Object
    {
        public Object(int netId, Vector3 position, Vector3 velocity)
        {
            this.netId = netId;
            this.position = position;
            this.velocity = velocity;
        }

        public int netId;
        public Vector3 position;
        public Vector3 velocity;
    }
    public class Data
    {
        public Data(int id)
        {
            this.id = id;
        }

        public int id;
        public List<Function> functions = new List<Function>();
        public List<Object> objects = new List<Object>();
    }

    public static Data Deserialize(byte[] data, ref int lastId)
    {
        MemoryStream stream = new MemoryStream(data);
        byte[] buffer = new byte[16];

        if (stream.Read(buffer, 0, INT_SIZE) == 0)
            return null;
        int id = BitConverter.ToInt32(buffer, 0);

        Data retData = new Data(id);

        if (stream.Read(buffer, 0, INT_SIZE) == 0)
            return null;
        Keyword type = (Keyword)BitConverter.ToInt32(buffer, 0);

        if (type == Keyword.FUNCTION)
        {
            if (stream.Read(buffer, 0, INT_SIZE) == 0)
                return null;
            int count = BitConverter.ToInt32(buffer, 0);
            if (count == 0)
                return null;

            for (int i = 0; i < count; ++i)
            {
                if (stream.Read(buffer, 0, INT_SIZE) == 0)
                    return null;
                Keyword functionType = (Keyword)BitConverter.ToInt32(buffer, 0);

                if (stream.Read(buffer, 0, INT_SIZE) == 0)
                    return null;
                int netId = BitConverter.ToInt32(buffer, 0);

                switch (functionType)
                {
                    case Keyword.FNC_NEW: // TODO: add "NEW_REPLY" function
                        if (stream.Read(buffer, 0, INT_SIZE) == 0)
                            return null;
                        Keyword objType = (Keyword)BitConverter.ToInt32(buffer, 0);

                        retData.functions.Add(new Function(functionType, netId, objType));
                        break;
                    case Keyword.FNC_DESTROY:
                        retData.functions.Add(new Function(functionType, netId, Keyword.NULL));
                        break;
                }
            }

            if (stream.Read(buffer, 0, INT_SIZE) == 0 || retData.id < lastId)
                return retData;
            type = (Keyword)BitConverter.ToInt32(buffer, 0);
        }

        if (type == Keyword.OBJECT)
        {
            if (stream.Read(buffer, 0, INT_SIZE) == 0)
                return null;
            int count = BitConverter.ToInt32(buffer, 0);
            if (count == 0)
                return null;

            for (int i = 0; i < count; ++i)
            {
                if (stream.Read(buffer, 0, INT_SIZE) == 0)
                    return null;
                int netId = BitConverter.ToInt32(buffer, 0);

                Vector3 position = new Vector3();
                if (stream.Read(buffer, 0, DOUBLE_SIZE) == 0)
                    return null;
                position.x = (float)BitConverter.ToDouble(buffer, 0);
                if (stream.Read(buffer, 0, DOUBLE_SIZE) == 0)
                    return null;
                position.y = (float)BitConverter.ToDouble(buffer, 0);
                if (stream.Read(buffer, 0, DOUBLE_SIZE) == 0)
                    return null;
                position.z = (float)BitConverter.ToDouble(buffer, 0);
                
                Vector3 velocity = new Vector3();
                if (stream.Read(buffer, 0, DOUBLE_SIZE) == 0)
                    return null;
                velocity.x = (float)BitConverter.ToDouble(buffer, 0);
                if (stream.Read(buffer, 0, DOUBLE_SIZE) == 0)
                    return null;
                velocity.y = (float)BitConverter.ToDouble(buffer, 0);
                if (stream.Read(buffer, 0, DOUBLE_SIZE) == 0)
                    return null;
                velocity.z = (float)BitConverter.ToDouble(buffer, 0);

                retData.objects.Add(new Object(netId, position, velocity));
            }
        }

        return retData;
    }
    // --- !Output --- 
}
