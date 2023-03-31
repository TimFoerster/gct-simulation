
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class CsvStreamWriter: StreamWriter
{
    public CsvStreamWriter(string path, bool append = false) : base(path, append)
    {

    }

    public override IFormatProvider FormatProvider
    {
        get
        {
            return CultureInfo.InvariantCulture;
        }
    }

    public void WriteLines(IEnumerable<string> lines)
    {
        Write(
            "\n" + string.Join("\n", lines)
        );
    }

    public System.Threading.Tasks.Task WriteLinesAsync(IEnumerable<string> lines)
    {
        return WriteAsync(
            "\n" + string.Join("\n", lines)
        );
    }

    public void WriteHeader(IEnumerable<string> fields)
    {
        Write(string.Join(",", fields));
    }

    public System.Threading.Tasks.Task WriteHeaderAsync(IEnumerable<string> fields)
    {
        return WriteAsync(string.Join(",", fields));
    }


}
