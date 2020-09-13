﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace PingCastle.RPC
{
    public class rprn : rpcapi
    {
        private static byte[] MIDL_ProcFormatStringx86 = new byte[]
        {
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70,
            0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x31, 0x04, 0x00, 0x00, 0x00, 0x5c, 0x08, 0x00, 0x40, 0x00, 0x46, 0x06, 0x08, 0x05,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x0b, 0x00, 0x00, 0x00, 0x02, 0x00, 0x10, 0x01, 0x04, 0x00, 0x0a, 0x00, 0x0b, 0x00, 0x08, 0x00, 0x02, 0x00, 0x0b, 0x01, 0x0c, 0x00, 0x1e,
            0x00, 0x48, 0x00, 0x10, 0x00, 0x08, 0x00, 0x70, 0x00, 0x14, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x08, 0x00, 0x32,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00,
            0x48, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00,
            0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x07, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01,
            0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x09, 0x00, 0x08, 0x00,
            0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00,
            0x00, 0x0a, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00,
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0b, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70,
            0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44,
            0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0e, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x00, 0x08,
            0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00,
            0x00, 0x00, 0x10, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x13, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00,
            0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x15, 0x00,
            0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x00, 0x16, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00,
            0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x17, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1a, 0x00, 0x08, 0x00, 0x32, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1b,
            0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48,
            0x00, 0x00, 0x00, 0x00, 0x1c, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04,
            0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1d, 0x00, 0x08, 0x00, 0x30, 0xe0, 0x00, 0x00, 0x00, 0x00, 0x38, 0x00, 0x40, 0x00, 0x44, 0x02, 0x08, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x18, 0x01, 0x00, 0x00, 0x36, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1e, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1f, 0x00,
            0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x00, 0x20, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00,
            0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x21, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x22, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x23, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x24, 0x00, 0x08, 0x00, 0x32, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x25,
            0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x26, 0x00,
            0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x27, 0x00, 0x08,
            0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00,
            0x00, 0x00, 0x28, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x29, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2b, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2c, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40,
            0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2d, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00,
            0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2e, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2f, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x30, 0x00, 0x08, 0x00, 0x32,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00,
            0x31, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x32,
            0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00,
            0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x00, 0x34, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00,
            0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x35, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x37, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x38, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x39, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40,
            0x00, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3a, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01,
            0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3b, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3c, 0x00, 0x08, 0x00,
            0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00,
            0x00, 0x3d, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x04, 0x00, 0x08, 0x00,
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3e, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70,
            0x00, 0x04, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3f, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x04, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x41, 0x00, 0x1c, 0x00, 0x30, 0x40, 0x00, 0x00, 0x00, 0x00, 0x3c, 0x00, 0x08, 0x00, 0x46, 0x07, 0x08, 0x05, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x3a, 0x00, 0x48, 0x00, 0x04, 0x00, 0x08, 0x00, 0x48, 0x00, 0x08, 0x00, 0x08, 0x00, 0x0b, 0x00, 0x0c, 0x00, 0x02, 0x00, 0x48,
            0x00, 0x10, 0x00, 0x08, 0x00, 0x0b, 0x00, 0x14, 0x00, 0x3e, 0x00, 0x70, 0x00, 0x18, 0x00, 0x08, 0x00, 0x00
        };

        private static byte[] MIDL_ProcFormatStringx64 = new byte[]
        {
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x30, 0x00, 0x31, 0x08, 0x00, 0x00, 0x00, 0x5c, 0x08, 0x00, 0x40, 0x00, 0x46, 0x06,
            0x0a, 0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b, 0x00, 0x00, 0x00, 0x02, 0x00, 0x10, 0x01, 0x08, 0x00, 0x0a, 0x00, 0x0b, 0x00, 0x10, 0x00, 0x02, 0x00, 0x0b,
            0x01, 0x18, 0x00, 0x1e, 0x00, 0x48, 0x00, 0x20, 0x00, 0x08, 0x00, 0x70, 0x00, 0x28, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x10, 0x00, 0x32, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00,
            0x00, 0x03, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00,
            0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01,
            0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x09, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0b,
            0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00,
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0e, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x0f, 0x00,
            0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00,
            0x48, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x13, 0x00, 0x10,
            0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48,
            0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70,
            0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x15, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x17, 0x00, 0x10, 0x00,
            0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x00, 0x18, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00,
            0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1a, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00,
            0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1b, 0x00, 0x10, 0x00, 0x32,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00,
            0x00, 0x00, 0x1c, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08,
            0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1d, 0x00, 0x10, 0x00, 0x30, 0xe0, 0x00, 0x00, 0x00, 0x00, 0x38, 0x00, 0x40, 0x00, 0x44, 0x02, 0x0a, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x01, 0x00, 0x00, 0x32, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x1e, 0x00, 0x10, 0x00, 0x32,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00,
            0x00, 0x00, 0x1f, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08,
            0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x21, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44,
            0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x22, 0x00, 0x10, 0x00, 0x32, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00,
            0x00, 0x23, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00,
            0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x24, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x25, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00,
            0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x26, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40,
            0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x27, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00,
            0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x10, 0x00, 0x32,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00,
            0x00, 0x00, 0x29, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08,
            0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2b, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40,
            0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2c, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2d, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2e, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x2f, 0x00, 0x10,
            0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48,
            0x00, 0x00, 0x00, 0x00, 0x30, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70,
            0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x31, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x32, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x34, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x35, 0x00,
            0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00,
            0x48, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x37, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x38, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x39, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3a, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01,
            0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3b, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00,
            0x3c, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3d, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3e, 0x00, 0x10, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x44, 0x01, 0x0a,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x3f, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x08, 0x00, 0x32, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x00, 0x41, 0x00, 0x38, 0x00, 0x30, 0x40,
            0x00, 0x00, 0x00, 0x00, 0x3c, 0x00, 0x08, 0x00, 0x46, 0x07, 0x0a, 0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x36, 0x00, 0x48, 0x00, 0x08,
            0x00, 0x08, 0x00, 0x48, 0x00, 0x10, 0x00, 0x08, 0x00, 0x0b, 0x00, 0x18, 0x00, 0x02, 0x00, 0x48, 0x00, 0x20, 0x00, 0x08, 0x00, 0x0b, 0x00, 0x28, 0x00, 0x3a, 0x00, 0x70, 0x00,
            0x30, 0x00, 0x08, 0x00, 0x00

        };

        private static byte[] MIDL_TypeFormatStringx86 = new byte[]
        {
            0x00, 0x00, 0x12, 0x08, 0x25, 0x5c, 0x11, 0x04, 0x02, 0x00, 0x30, 0xa0, 0x00, 0x00, 0x11, 0x00, 0x0e, 0x00, 0x1b, 0x00, 0x01, 0x00, 0x19, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01,
            0x5b, 0x16, 0x03, 0x08, 0x00, 0x4b, 0x5c, 0x46, 0x5c, 0x04, 0x00, 0x04, 0x00, 0x12, 0x00, 0xe6, 0xff, 0x5b, 0x08, 0x08, 0x5b, 0x11, 0x04, 0x02, 0x00, 0x30, 0xe1, 0x00, 0x00,
            0x30, 0x41, 0x00, 0x00, 0x12, 0x00, 0x48, 0x00, 0x1b, 0x01, 0x02, 0x00, 0x19, 0x00, 0x0c, 0x00, 0x01, 0x00, 0x06, 0x5b, 0x16, 0x03, 0x14, 0x00, 0x4b, 0x5c, 0x46, 0x5c, 0x10,
            0x00, 0x10, 0x00, 0x12, 0x00, 0xe6, 0xff, 0x5b, 0x06, 0x06, 0x08, 0x08, 0x08, 0x08, 0x5b, 0x1b, 0x03, 0x14, 0x00, 0x19, 0x00, 0x08, 0x00, 0x01, 0x00, 0x4b, 0x5c, 0x48, 0x49,
            0x14, 0x00, 0x00, 0x00, 0x01, 0x00, 0x10, 0x00, 0x10, 0x00, 0x12, 0x00, 0xc2, 0xff, 0x5b, 0x4c, 0x00, 0xc9, 0xff, 0x5b, 0x16, 0x03, 0x10, 0x00, 0x4b, 0x5c, 0x46, 0x5c, 0x0c,
            0x00, 0x0c, 0x00, 0x12, 0x00, 0xd0, 0xff, 0x5b, 0x08, 0x08, 0x08, 0x08, 0x5b, 0x00
        };

        private static byte[] MIDL_TypeFormatStringx64 = new byte[]
        {
            0x00, 0x00, 0x12, 0x08, 0x25, 0x5c, 0x11, 0x04, 0x02, 0x00, 0x30, 0xa0, 0x00, 0x00, 0x11, 0x00, 0x0e, 0x00, 0x1b, 0x00, 0x01, 0x00, 0x19, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01,
            0x5b, 0x1a, 0x03, 0x10, 0x00, 0x00, 0x00, 0x06, 0x00, 0x08, 0x40, 0x36, 0x5b, 0x12, 0x00, 0xe6, 0xff, 0x11, 0x04, 0x02, 0x00, 0x30, 0xe1, 0x00, 0x00, 0x30, 0x41, 0x00, 0x00,
            0x12, 0x00, 0x38, 0x00, 0x1b, 0x01, 0x02, 0x00, 0x19, 0x00, 0x0c, 0x00, 0x01, 0x00, 0x06, 0x5b, 0x1a, 0x03, 0x18, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x06, 0x06, 0x08, 0x08, 0x08,
            0x36, 0x5c, 0x5b, 0x12, 0x00, 0xe2, 0xff, 0x21, 0x03, 0x00, 0x00, 0x19, 0x00, 0x08, 0x00, 0x01, 0x00, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x4c, 0x00, 0xda, 0xff, 0x5c, 0x5b,
            0x1a, 0x03, 0x18, 0x00, 0x00, 0x00, 0x08, 0x00, 0x08, 0x08, 0x08, 0x40, 0x36, 0x5b, 0x12, 0x00, 0xda, 0xff, 0x00
        };

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public rprn()
        {
            Guid interfaceId = new Guid("12345678-1234-ABCD-EF00-0123456789AB");
            if (IntPtr.Size == 8)
            {
                InitializeStub(interfaceId, MIDL_ProcFormatStringx64, MIDL_TypeFormatStringx64, "\\pipe\\spoolss");
            }
            else
            {
                InitializeStub(interfaceId, MIDL_ProcFormatStringx86, MIDL_TypeFormatStringx86, "\\pipe\\spoolss");
            }
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        ~rprn()
        {
            freeStub();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DEVMODE_CONTAINER
        {
            int cbBuf;
            IntPtr pDevMode;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RPC_V2_NOTIFY_OPTIONS_TYPE
        {
            ushort Type;
            ushort Reserved0;
            uint Reserved1;
            uint Reserved2;
            uint Count;
            IntPtr pFields;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RPC_V2_NOTIFY_OPTIONS
        {
            uint Version;
            uint Reserved;
            uint Count;
            /* [unique][size_is] */
            RPC_V2_NOTIFY_OPTIONS_TYPE pTypes;
        };

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public int RpcOpenPrinter(string pPrinterName, out IntPtr pHandle, string pDatatype, ref DEVMODE_CONTAINER pDevModeContainer, int AccessRequired)
        {
            IntPtr result = IntPtr.Zero;
            IntPtr intptrPrinterName = Marshal.StringToHGlobalUni(pPrinterName);
            IntPtr intptrDatatype = Marshal.StringToHGlobalUni(pDatatype);
            pHandle = IntPtr.Zero;
            try
            {
                if (IntPtr.Size == 8)
                {
                    result = NativeMethods.NdrClientCall2x64(GetStubHandle(), GetProcStringHandle(36), pPrinterName, out pHandle, pDatatype, ref pDevModeContainer, AccessRequired);
                }
                else
                {
                    IntPtr tempValue = IntPtr.Zero;
                    GCHandle handle = GCHandle.Alloc(tempValue, GCHandleType.Pinned);
                    IntPtr tempValuePointer = handle.AddrOfPinnedObject();
                    GCHandle handleDevModeContainer = GCHandle.Alloc(pDevModeContainer, GCHandleType.Pinned);
                    IntPtr tempValueDevModeContainer = handleDevModeContainer.AddrOfPinnedObject();
                    try
                    {
                        result = CallNdrClientCall2x86(34, intptrPrinterName, tempValuePointer, intptrDatatype, tempValueDevModeContainer, new IntPtr(AccessRequired));

                        // each pinvoke work on a copy of the arguments (without an out specifier)
                        // get back the data
                        pHandle = Marshal.ReadIntPtr(tempValuePointer);
                    }
                    finally
                    {
                        handle.Free();
                        handleDevModeContainer.Free();
                    }
                }
            }
            catch (SEHException)
            {
                Trace.WriteLine("RpcOpenPrinter failed 0x" + Marshal.GetExceptionCode().ToString("x"));
                return Marshal.GetExceptionCode();
            }
            finally
            {
                if (intptrPrinterName != IntPtr.Zero)
                    Marshal.FreeHGlobal(intptrPrinterName);
                if (intptrDatatype != IntPtr.Zero)
                    Marshal.FreeHGlobal(intptrDatatype);
            }
            return (int)result.ToInt64();
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public int RpcClosePrinter(ref IntPtr ServerHandle)
        {
            IntPtr result = IntPtr.Zero;
            try
            {
                if (IntPtr.Size == 8)
                {
                    result = NativeMethods.NdrClientCall2x64(GetStubHandle(), GetProcStringHandle(1076), ref ServerHandle);
                }
                else
                {
                    IntPtr tempValue = ServerHandle;
                    GCHandle handle = GCHandle.Alloc(tempValue, GCHandleType.Pinned);
                    IntPtr tempValuePointer = handle.AddrOfPinnedObject();
                    try
                    {
                        result = CallNdrClientCall2x86(1018, tempValuePointer);

                        // each pinvoke work on a copy of the arguments (without an out specifier)
                        // get back the data
                        ServerHandle = Marshal.ReadIntPtr(tempValuePointer);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch (SEHException)
            {
                Trace.WriteLine("RpcClosePrinter failed 0x" + Marshal.GetExceptionCode().ToString("x"));
                return Marshal.GetExceptionCode();
            }
            return (int)result.ToInt64();
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public int RpcRemoteFindFirstPrinterChangeNotificationEx(
            /* [in] */ IntPtr hPrinter,
                       /* [in] */
                       uint fdwFlags,
                       /* [in] */
                       uint fdwOptions,
                       /* [unique][string][in] */
                       string pszLocalMachine,
                       /* [in] */
                       uint dwPrinterLocal)
        {
            IntPtr result = IntPtr.Zero;
            IntPtr intptrLocalMachine = Marshal.StringToHGlobalUni(pszLocalMachine);
            try
            {
                if (IntPtr.Size == 8)
                {
                    result = NativeMethods.NdrClientCall2x64(GetStubHandle(), GetProcStringHandle(2308), hPrinter, fdwFlags, fdwOptions, pszLocalMachine, dwPrinterLocal, IntPtr.Zero);
                }
                else
                {
                    try
                    {
                        result = CallNdrClientCall2x86(2178, hPrinter, new IntPtr(fdwFlags), new IntPtr(fdwOptions), intptrLocalMachine, new IntPtr(dwPrinterLocal), IntPtr.Zero);

                        // each pinvoke work on a copy of the arguments (without an out specifier)
                        // get back the data
                    }
                    finally { }
                }
            }
            catch (SEHException)
            {
                Trace.WriteLine("RpcRemoteFindFirstPrinterChangeNotificationEx failed 0x" + Marshal.GetExceptionCode().ToString("x"));
                return Marshal.GetExceptionCode();
            }
            finally
            {
                if (intptrLocalMachine != IntPtr.Zero)
                    Marshal.FreeHGlobal(intptrLocalMachine);
            }
            return (int)result.ToInt64();
        }
    }
}