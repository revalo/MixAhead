/*
 * Adapted from https://github.com/nccgroup/memscan
 * Originally written by http://www.nccgroup.com/
 * Modified by reval
 * 
*/

using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace MemoryScanner
{
    public class MemoryScanner
    {
        // REQUIRED CONSTS
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int MEM_COMMIT = 0x00001000;
        const int PAGE_READWRITE = 0x04;
        const int PROCESS_WM_READ = 0x0010;

        // REQUIRED METHODS
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        static Process process;

        // REQUIRED STRUCTS
        public struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public int AllocationProtect;
            public int RegionSize;
            public int State;
            public int Protect;
            public int lType;
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        public MemoryScanner(Process p)
        {
            process = p;
        }

        public string[] ScanRegex(String searchterm)
        {
            // Assemble Regex
            Regex rgx = new Regex(searchterm);
      
            // getting minimum & maximum address
            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            IntPtr proc_min_address = sys_info.minimumApplicationAddress;
            IntPtr proc_max_address = sys_info.maximumApplicationAddress;

            // saving the values as long ints to avoid  lot of casts later
            long proc_min_address_l = (long)proc_min_address;
            long proc_max_address_l = (long)proc_max_address;

            String toSend = "";

            // opening the process with desired access level
            IntPtr processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, process.Id);

            // this will store any information we get from VirtualQueryEx()
            MEMORY_BASIC_INFORMATION mem_basic_info = new MEMORY_BASIC_INFORMATION();

            // number of bytes read with ReadProcessMemory
            int bytesRead = 0;

            // for some efficiencies, pre-compute prepostfix values
            int postfix = searchterm.Length;

            // Store results
            List<String> ret = new List<String>();

            while (proc_min_address_l < proc_max_address_l)
            {
                // 28 = sizeof(MEMORY_BASIC_INFORMATION)
                VirtualQueryEx(processHandle, proc_min_address, out mem_basic_info, 28);

                // if this memory chunk is accessible
                if (mem_basic_info.Protect == PAGE_READWRITE && mem_basic_info.State == MEM_COMMIT)
                {
                    byte[] buffer = new byte[mem_basic_info.RegionSize];

                    // read everything in the buffer above
                    ReadProcessMemory((int)processHandle, mem_basic_info.BaseAddress, buffer, mem_basic_info.RegionSize, ref bytesRead);

                    String memStringASCII = Encoding.ASCII.GetString(buffer);
                    String memStringUNICODE = Encoding.Unicode.GetString(buffer);

                    // does the regex pattern exist in this chunk in ASCII form?
                    if (rgx.IsMatch(memStringASCII))
                    {
                        int idex = 0;
                        while (rgx.Match(memStringASCII, idex).Success)
                        {
                            idex = rgx.Match(memStringASCII, idex).Index;
                            toSend += "0x" + (mem_basic_info.BaseAddress + idex).ToString() + ":A:" + memStringASCII.Substring(idex, postfix) + "\n";

                           // ret.Add(toSend);

                            toSend = "";
                            idex++;
                        }
                    }

                    // does the regex pattern exist in this chunk in UNICODE form?
                    if (rgx.IsMatch(memStringUNICODE))
                    {

                        int idex = 0;
                        while (rgx.Match(memStringUNICODE, idex).Success)
                        {
                            idex = rgx.Match(memStringUNICODE, idex).Index;
                            toSend += memStringUNICODE.Substring(idex, rgx.Match(memStringUNICODE, idex).Length);

                            ret.Add(toSend);

                            toSend = "";
                            idex++;
                        }
                    }
                }

                // truffle shuffle - moving on chunk
                proc_min_address_l += mem_basic_info.RegionSize;
                proc_min_address = new IntPtr(proc_min_address_l);
            }

            return ret.ToArray();
        }
    }
}
