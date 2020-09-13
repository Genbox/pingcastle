﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PingCastle.RPC
{
    public class OxidBindings : rpcapi
    {
        private static byte[] MIDL_ProcFormatStringx86 = new byte[]
        {
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x48, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48,
            0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x00, 0x03, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x01, 0x00, 0x00,
            0x00, 0x05, 0x00, 0x14, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4c, 0x00, 0x45, 0x04, 0x08, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x21, 0x04, 0x00, 0x06, 0x00,
            0x13, 0x20, 0x08, 0x00, 0x0e, 0x00, 0x50, 0x21, 0x0c, 0x00, 0x08, 0x00, 0x70, 0x00, 0x10, 0x00, 0x10, 0x00, 0x00
        };

        private static byte[] MIDL_ProcFormatStringx64 = new byte[]
        {
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x01, 0x00, 0x00, 0x00, 0x05, 0x00, 0x28, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4c, 0x00, 0x45, 0x04, 0x0a, 0x03, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x21, 0x08, 0x00, 0x06, 0x00, 0x13, 0x20, 0x10, 0x00, 0x0e, 0x00, 0x50, 0x21, 0x18, 0x00, 0x08, 0x00, 0x70, 0x00, 0x20, 0x00, 0x10,
            0x00, 0x00
        };

        private static byte[] MIDL_TypeFormatStringx86 = new byte[]
        {
            0x00, 0x00, 0x11, 0x04, 0x02, 0x00, 0x15, 0x01, 0x04, 0x00, 0x06, 0x06, 0x5c, 0x5b, 0x11, 0x14, 0x02, 0x00, 0x12, 0x00, 0x0e, 0x00, 0x1b, 0x01, 0x02, 0x00, 0x07, 0x00, 0xfc,
            0xff, 0x01, 0x00, 0x06, 0x5b, 0x17, 0x01, 0x04, 0x00, 0xf0, 0xff, 0x06, 0x06, 0x5c, 0x5b, 0x11, 0x0c, 0x08, 0x5c, 0x00
        };

        private static byte[] MIDL_TypeFormatStringx64 = new byte[]
        {
            0x00, 0x00, 0x11, 0x04, 0x02, 0x00, 0x15, 0x01, 0x04, 0x00, 0x06, 0x06, 0x5c, 0x5b, 0x11, 0x14, 0x02, 0x00, 0x12, 0x00, 0x0e, 0x00, 0x1b, 0x01, 0x02, 0x00, 0x07, 0x00, 0xfc,
            0xff, 0x01, 0x00, 0x06, 0x5b, 0x17, 0x01, 0x04, 0x00, 0xf0, 0xff, 0x06, 0x06, 0x5c, 0x5b, 0x11, 0x0c, 0x08, 0x5c, 0x00
        };

        public OxidBindings()
        {
            Guid interfaceId = new Guid("99fcfec4-5260-101b-bbcb-00aa0021347a");
            if (IntPtr.Size == 8)
            {
                InitializeStub(interfaceId, MIDL_ProcFormatStringx64, MIDL_TypeFormatStringx64, null, 0);
            }
            else
            {
                InitializeStub(interfaceId, MIDL_ProcFormatStringx86, MIDL_TypeFormatStringx86, null, 0);
            }
        }

        ~OxidBindings()
        {
            freeStub();
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMVERSION
        {
            public UInt16 MajorVersion;
            public UInt16 MinorVersion;
        }

        public Int32 ServerAlive2(string server, out List<string> stringBindings)
        {
            IntPtr hBind;
            stringBindings = new List<string>();
            Int32 status = Bind(server, out hBind);
            if (status != 0)
                return status;
            try
            {
                status = NativeMethods.RpcEpResolveBinding(hBind, rpcClientInterface);
                if (status != 0)
                    return status;

                var conversion = new COMVERSION() { MajorVersion = 5, MinorVersion = 1 };
                UInt32 reserved = 0;
                IntPtr DualStringArray = IntPtr.Zero;
                try
                {
                    if (IntPtr.Size == 8)
                    {
                        IntPtr result = NativeMethods.NdrClientCall2x64(GetStubHandle(), GetProcStringHandle(150), hBind, ref conversion, out DualStringArray, ref reserved);
                        if (result != IntPtr.Zero)
                            return result.ToInt32();
                    }
                    else
                    {
                        GCHandle h2 = GCHandle.Alloc(conversion, GCHandleType.Pinned);

                        GCHandle h3 = GCHandle.Alloc(DualStringArray, GCHandleType.Pinned);
                        GCHandle h4 = GCHandle.Alloc(reserved, GCHandleType.Pinned);
                        IntPtr tempValuePointer = h3.AddrOfPinnedObject();
                        try
                        {
                            IntPtr result = CallNdrClientCall2x86(140, hBind, h2.AddrOfPinnedObject(), tempValuePointer, h4.AddrOfPinnedObject());
                            if (result != IntPtr.Zero)
                                return result.ToInt32();

                            // each pinvoke work on a copy of the arguments (without an out specifier)
                            // get back the data
                            DualStringArray = Marshal.ReadIntPtr(tempValuePointer);

                        }
                        finally
                        {
                            h2.Free();
                            h3.Free();
                            h4.Free();
                        }
                    }
                    Int16 wSecurityOffest = Marshal.ReadInt16(new IntPtr(DualStringArray.ToInt64() + 2));
                    int offset = 4;
                    while (offset < wSecurityOffest * 2)
                    {
                        string value = Marshal.PtrToStringUni(new IntPtr(DualStringArray.ToInt64() + offset + 2));
                        stringBindings.Add(value);
                        offset += value.Length * 2 + 2 + 2;
                    }
                    FreeMemory(DualStringArray);
                }
                catch (SEHException)
                {
                    return Marshal.GetExceptionCode();
                }
            }
            finally
            {
                Unbind(IntPtr.Zero, hBind);
            }
            return 0;
        }
    }
}