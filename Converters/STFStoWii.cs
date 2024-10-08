﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameArchives.STFS;
using GameArchives;
using DtxCS.DataTypes;
using DtxCS;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Forms;

namespace LavaManager.Converters
{
    class STFStoWii
    {
        private STFSPackage stfs;
        private DataArray songs;
        private string tempPath;
        private string output;
        private readonly string wimgt;

        public STFStoWii(string stfsPath, string outPath)
        {
            stfs = STFSPackage.OpenFile(Util.LocalFile(stfsPath));
            output = outPath;
            songs = DTX.FromPlainTextBytes(stfs.GetFile("songs/songs.dta").GetBytes());
            if (songs.Count > 1) // if we have multiple songs, use the name of the STFS for our output
                tempPath = Program.TempDir + stfs.FileName;
            else
                tempPath = Program.TempDir + songs.Array(0).Name;
            Directory.CreateDirectory(tempPath);
            wimgt = Application.StartupPath + "\\wimgt.exe";
            
        }

        public int SongCount()
        {
            return songs.Count;
        }


        public void CopyDTA()
        {
            Directory.CreateDirectory($"{tempPath}/lava/dta");
            for(int i = 0; i < songs.Count; i++)
            {
                songs.Array(i).Array("version")[1] = new DataAtom(1);
                File.WriteAllText($"{tempPath}/lava/dta/{Shortname(i)}.dta", songs.Array(i).ToString());
            }
        }

        public string Shortname(int songIndex)
        {
            return songs.Array(songIndex).Array("song").Array("name").String(1).Split('/')[1];
            //return songs.Array(songIndex).Name;
        }

        public void ConvertMOGG(int songIndex)
        {
            // TODO: downmix for less CPU load, or convert to BIK
            // right now this just copies.
            string shortname = Shortname(songIndex);
            Directory.CreateDirectory($"{tempPath}/songs/{shortname}/gen");
            File.WriteAllBytes($"{tempPath}/songs/{shortname}/{shortname}.mogg", stfs.GetFile($"songs/{shortname}/{shortname}.mogg").GetBytes());
        }

        public void ConvertMILO(int songIndex)
        {
            string shortname = Shortname(songIndex);
            Directory.CreateDirectory($"{tempPath}/songs/{shortname}/gen");
            File.WriteAllBytes($"{tempPath}/songs/{shortname}/gen/{shortname}.milo_wii", stfs.GetFile($"songs/{shortname}/gen/{shortname}.milo_xbox").GetBytes());
        }

        public void ConvertMID(int songIndex)
        {
            string shortname = Shortname(songIndex);
            Directory.CreateDirectory($"{tempPath}/songs/{shortname}/gen");
            RBN2toRBN1 rbnConverter = new RBN2toRBN1(stfs.GetFile($"songs/{shortname}/{shortname}.mid").Stream, $"{tempPath}/songs/{shortname}/{shortname}.mid");
            try
            { // try to convert the MIDI, if successful don't copy
                if (rbnConverter.FixMIDI()) return;
            } catch (Exception) {}
            // we failed, copy the MIDI as-is
            File.WriteAllBytes($"{tempPath}/songs/{shortname}/{shortname}.mid", stfs.GetFile($"songs/{shortname}/{shortname}.mid").GetBytes());
        }

        public void ConvertPNG(int songIndex)
        {
            string shortname = Shortname(songIndex);
            // Directory.CreateDirectory($"{tempPath}/songs/{shortname}/gen"); // should have already been created a this point
            File.WriteAllBytes($"{tempPath}/songs/{shortname}/gen/{shortname}_keep.png_xbox", stfs.GetFile($"songs/{shortname}/gen/{shortname}_keep.png_xbox").GetBytes());
            XboxPNGtoWiiPNG xboxPNGtoWiiPNG = new XboxPNGtoWiiPNG();
            xboxPNGtoWiiPNG.ConvertImagetoWii(wimgt, $"{tempPath}/songs/{shortname}/gen/{shortname}_keep.png_xbox", $"{tempPath}/songs/{shortname}/gen/{shortname}_keep.png_wii", true);
            Directory.Delete($"{tempPath}/songs/{shortname}/gen/converted", true);
        }

        public void DestroyTemp()
        {
            Directory.Delete(tempPath);
        }

        public void Finalise()
        {
            FileSystem.MoveDirectory(tempPath, output, true);
            //DestroyTemp();
        }
    }
}
