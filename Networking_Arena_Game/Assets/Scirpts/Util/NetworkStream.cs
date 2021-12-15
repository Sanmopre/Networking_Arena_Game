using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

public class NetworkStream
{
    public NetworkStream()
    {

    }
    public NetworkStream(Data data)
    {
        AddIdData(data.id);

        for (int i = 0; i < data.functions.Count; ++i)
            AddFunction(data.functions[i].functionType, data.functions[i].netId, data.functions[i].owned, data.functions[i].position, data.functions[i].velocity, data.functions[i].damage);
        for (int i = 0; i < data.objects.Count; ++i)
            AddObject(data.objects[i].netId, data.objects[i].position, data.objects[i].velocity, data.objects[i].direction);
    }

    ~NetworkStream()
    {
        idData.Dispose();
        fncData.Dispose();
        objData.Dispose();
    }
    // --- Input --- 
    MemoryStream idData = new MemoryStream();
    MemoryStream fncData = new MemoryStream();
    MemoryStream objData = new MemoryStream();

    int fncCount = 0;
    int objCount = 0;

    readonly static int INT_SIZE = sizeof(int);
    readonly static int DOUBLE_SIZE = sizeof(double);
    readonly static int BOOL_SIZE = sizeof(bool);
    public enum Keyword
    {
        NULL = -1,
        OBJECT = 0,
        FUNCTION = 1,
        FNC_BULLET = 2,
        FNC_MISSILE = 3,
        FNC_SHOTGUN = 4,
        FNC_HIT = 5
    }

    public void AddIdData(int id)
    {
        idData.Write(BitConverter.GetBytes(id), 0, INT_SIZE);
    }

    void AddFunctionHeader(Keyword type, int netId, bool owned)
    {
        fncData.Write(BitConverter.GetBytes((int)type), 0, INT_SIZE);
        fncData.Write(BitConverter.GetBytes(netId), 0, INT_SIZE);
        fncData.Write(BitConverter.GetBytes(owned), 0, BOOL_SIZE);
    }

    void AddFunction(Keyword type, int netId, bool owned, Vector3 position, Vector3 velocity, int damage)
    {
        AddFunctionHeader(type, netId, owned);

        fncData.Write(BitConverter.GetBytes((double)position.x), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.y), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.z), 0, DOUBLE_SIZE);

        fncData.Write(BitConverter.GetBytes((double)velocity.x), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)velocity.y), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)velocity.z), 0, DOUBLE_SIZE);

        fncData.Write(BitConverter.GetBytes(damage), 0, INT_SIZE);
    }

    public void AddBulletFunction(int netId, bool owned, Vector3 position, Vector3 velocity)
    {
        AddFunctionHeader(Keyword.FNC_BULLET, netId, owned);

        fncData.Write(BitConverter.GetBytes((double)position.x), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.y), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.z), 0, DOUBLE_SIZE);

        fncData.Write(BitConverter.GetBytes((double)velocity.x), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)velocity.y), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)velocity.z), 0, DOUBLE_SIZE);

        ++fncCount;
    }

    public void AddMissileFunction(int netId, bool owned, Vector3 position, float time)
    {
        AddFunctionHeader(Keyword.FNC_MISSILE, netId, owned);

        fncData.Write(BitConverter.GetBytes((double)position.x), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.y), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.z), 0, DOUBLE_SIZE);

        fncData.Write(BitConverter.GetBytes((double)time), 0, DOUBLE_SIZE);

        ++fncCount;
    }

    public void AddShotgunFunction(int netId, bool owned, Vector3 position, Vector3 direction)
    {
        AddFunctionHeader(Keyword.FNC_SHOTGUN, netId, owned);

        fncData.Write(BitConverter.GetBytes((double)position.x), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.y), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)position.z), 0, DOUBLE_SIZE);

        fncData.Write(BitConverter.GetBytes((double)direction.x), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)direction.y), 0, DOUBLE_SIZE);
        fncData.Write(BitConverter.GetBytes((double)direction.z), 0, DOUBLE_SIZE);

        ++fncCount;
    }

    public void AddHitFunction(int netId, bool owned, int damage)
    {
        AddFunctionHeader(Keyword.FNC_HIT, netId, owned);

        fncData.Write(BitConverter.GetBytes(damage), 0, INT_SIZE);

        ++fncCount;
    }

    public void AddObject(int netId, Vector3 position, Vector3 velocity, Vector3 direction)
    {
        objData.Write(BitConverter.GetBytes(netId), 0, INT_SIZE);

        objData.Write(BitConverter.GetBytes((double)position.x), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)position.y), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)position.z), 0, DOUBLE_SIZE);

        objData.Write(BitConverter.GetBytes((double)velocity.x), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)velocity.y), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)velocity.z), 0, DOUBLE_SIZE);

        objData.Write(BitConverter.GetBytes((double)direction.x), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)direction.y), 0, DOUBLE_SIZE);
        objData.Write(BitConverter.GetBytes((double)direction.z), 0, DOUBLE_SIZE);

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
        public Function(Keyword functionType, int netId, bool owned, Vector3 position, Vector3 velocity, float time, int damage)
        {
            this.functionType = functionType;
            this.netId = netId;
            this.owned = owned;

            this.position = position;
            this.velocity = velocity;

            this.time = time;

            this.damage = damage;
        }

        // FUNCTION
        public Keyword functionType;
        public int netId;
        public bool owned;
        // BULLET
        public Vector3 position;
        public Vector3 velocity;
        // MISSILE
        public float time;
        // HIT
        public int damage;
    }
    public struct Object
    {
        public Object(int netId, Vector3 position, Vector3 velocity, Vector3 direction)
        {
            this.netId = netId;
            this.position = position;
            this.velocity = velocity;
            this.direction = direction;
        }

        public int netId;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 direction;
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

    public static Data Deserialize(byte[] data)
    {
        OutputStream stream = new OutputStream(data);
        byte[] buffer = new byte[16];

        int id = stream.GetInt();

        Data retData = new Data(id);

        Keyword type = (Keyword)stream.GetInt();

        if (type == Keyword.FUNCTION)
        {
            int count = stream.GetInt();
            if (count == 0)
                return null;

            for (int i = 0; i < count; ++i)
            {
                Keyword functionType = (Keyword)stream.GetInt();
                int netId = stream.GetInt();
                bool owned = stream.GetBool();

                switch (functionType)
                {
                    case Keyword.FNC_BULLET:
                        {
                            Vector3 position = stream.GetVector3();
                            Vector3 velocity = stream.GetVector3();

                            retData.functions.Add(new Function(functionType, netId, owned, position, velocity, 0, 0));
                        }
                        break;
                    case Keyword.FNC_MISSILE:
                        {
                            Vector3 position = stream.GetVector3();
                            float time = stream.GetFloat();

                            retData.functions.Add(new Function(functionType, netId, owned, position, Vector3.zero, time, 0));
                        }
                        break;
                    case Keyword.FNC_SHOTGUN:
                        {
                            Vector3 position = stream.GetVector3();
                            Vector3 velocity = stream.GetVector3();

                            retData.functions.Add(new Function(functionType, netId, owned, position, velocity, 0, 0));
                        }
                        break;
                    case Keyword.FNC_HIT:
                        int damage = stream.GetInt();

                        retData.functions.Add(new Function(functionType, netId, owned, Vector3.zero, Vector3.zero, 0, damage));
                        break;
                }
            }

            type = (Keyword)stream.GetInt();
        }

        if (type == Keyword.OBJECT)
        {
            int count = stream.GetInt();
            if (count == 0)
                return null;

            for (int i = 0; i < count; ++i)
            {
                int netId = stream.GetInt();

                Vector3 position = stream.GetVector3();
                Vector3 velocity = stream.GetVector3();
                Vector3 direction = stream.GetVector3();

                retData.objects.Add(new Object(netId, position, velocity, direction));
            }
        }

        return retData;
    }
    // --- !Output --- 
}

public class InputStream
{
    MemoryStream stream = new MemoryStream();

    public InputStream() { }

    public void AddInt(int i)
    {
        stream.Write(BitConverter.GetBytes(i), 0, sizeof(int));
    }

    public void AddFloat(float f)
    {
        stream.Write(BitConverter.GetBytes((double)f), 0, sizeof(double));
    }

    public void AddVector3(Vector3 v)
    {
        AddFloat(v.x);
        AddFloat(v.y);
        AddFloat(v.z);
    }

    public void AddBool(bool b)
    {
        stream.Write(BitConverter.GetBytes(b), 0, sizeof(bool));
    }

    public void AddString(string s)
    {
        stream.Write(Encoding.UTF8.GetBytes(s), 0, s.Length);
    }

    public void AddBytes(byte[] b)
    {
        stream.Write(b, 0, b.Length);
    }

    public byte[] GetBuffer(bool addEnd = false)
    {
        if (addEnd)
            AddInt(-2);
        byte[] buffer = stream.GetBuffer();
        return ByteArray.TrimEnd(buffer);
    }
}
public class OutputStream
{
    MemoryStream stream = null;
    byte[] buffer = new byte[64];

    private OutputStream() { }

    public OutputStream(byte[] str)
    {
        stream = new MemoryStream(str);
    }

    public int GetInt()
    {
        stream.Read(buffer, 0, sizeof(int));
        return BitConverter.ToInt32(buffer, 0);
    }

    public float GetFloat()
    {
        stream.Read(buffer, 0, sizeof(double));
        return (float)BitConverter.ToDouble(buffer, 0);
    }

    public Vector3 GetVector3()
    {
        return new Vector3(GetFloat(), GetFloat(), GetFloat());
    }

    public bool GetBool()
    {
        stream.Read(buffer, 0, sizeof(bool));
        return BitConverter.ToBoolean(buffer, 0);
    }

    public string GetString(int size)
    {
        byte[] strBuff = new byte[size];
        stream.Read(strBuff, 0, size);
        return Encoding.UTF8.GetString(strBuff);
    }

    public byte[] GetBytes(int size)
    {
        byte[] strBuff = new byte[size];
        stream.Read(strBuff, 0, size);
        return strBuff;
    }

    public bool ReachedEnd()
    {
        if (stream.Position >= stream.Length - 1)
            return true;
        return false;
    }
}