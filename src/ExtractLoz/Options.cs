﻿/*
   Copyright 2016 Aldo J. Nunez

   Licensed under the Apache License, Version 2.0.
   See the LICENSE text file for details.
*/

// This file has been modified by Joseph Lennox 2014

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace ExtractLoz
{
    class Options
    {
        public string RomPath;
        public string Function;
        public string Error;
        public bool AnalysisWrites;
        public byte[] RomHash;

        public Dictionary<string, byte[]> Files = new();

        public static Options Parse( string[] args )
        {
            Options options = new Options();

            if ( args.Length < 2 )
                return options;

            options.RomPath = args[0];
            options.Function = args[1].ToLowerInvariant();

            return options;
        }

        public BinaryReader GetBinaryReader()
        {
            return new BinaryReader(File.OpenRead(RomPath));
        }

        public void AddFile(string relativePath, byte[] data)
        {
            Files.Add(relativePath, data);
        }

        public void AddFile(string relativePath, Stream data)
        {
            using var ms = new MemoryStream();
            data.CopyTo(ms);
            Files.Add(relativePath, ms.ToArray());
        }

        public void AddFile(string relativePath, MemoryStream data)
        {
            Files[relativePath] = data.ToArray();
        }

        public void AddFile(string relativePath, Bitmap bitmap, ImageFormat imageformat)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, imageformat);
            Files.Add(relativePath, ms.ToArray());
        }

        public OptionsStream AddStream(string relativePath)
        {
            return new OptionsStream(relativePath, this);
        }

        public OptionsTempFile AddTempFile(string relativePath)
        {
            return new OptionsTempFile(relativePath, this);
        }
    }

    class OptionsStream : MemoryStream
    {
        private readonly string _filename;
        private readonly Options _options;
        private bool _closed = false;

        public OptionsStream(string filename, Options options)
        {
            _filename = filename;
            _options = options;
        }

        public new void Dispose()
        {
            if (_closed) return;
            _closed = true;
            _options.AddFile(_filename, this);
            base.Dispose();
        }

        public override void Close()
        {
            Dispose();
            base.Close();
        }
    }

    class OptionsTempFile : IDisposable
    {
        public readonly string TempFilename;

        private readonly string _filename;
        private readonly Options _options;

        public OptionsTempFile(string filename, Options options)
        {
            TempFilename = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            _filename = filename;
            _options = options;
        }

        public void Dispose()
        {
            using var stream = File.OpenRead(TempFilename);
            _options.AddFile(_filename, stream);
        }
    }
}
