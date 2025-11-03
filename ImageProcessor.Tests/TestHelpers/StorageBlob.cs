using System;

namespace ImageProcessor.Tests.TestHelpers;

public class StorageBlob
{
    private readonly string _name;
    private string _status = string.Empty;

    public StorageBlob(string name)
    {
        _name = name;
    }

    public string Name
    {
        get { return _name; }
    }

    public string Status
    {
        get { return _status; }
        set { _status = value; }
    }
}
