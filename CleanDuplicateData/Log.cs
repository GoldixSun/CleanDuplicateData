using System;
using System.IO;

namespace CleanDuplicateData
{
    class Log : IDisposable
    {
        private bool _diposed = false;
        private FileStream _stream;
        private StreamWriter _writer;

        public Log(string path)
        {
            _stream = new FileStream(path, FileMode.OpenOrCreate);
            _stream.Seek(0, SeekOrigin.End);
            _writer = new StreamWriter(_stream);

            FileName = path;
        }

        public string FileName { get; private set; }

        ~Log() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_diposed) return;

            if (disposing)
            {
                _writer.Close();
                _stream.Close();

                _stream.Dispose();
                _writer.Dispose();

                FileName = "";
            }

            _diposed = true;
        }

        public void Write(string message)
        {
            _writer.WriteLine(message);
        }
    }

}
