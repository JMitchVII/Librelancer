﻿﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Security;
using System.Runtime.InteropServices;
namespace LibreLancer {
	static partial class SSEMath {
	#pragma warning disable 414
		[SuppressUnmanagedCodeSecurity]
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MatrixInvertDelegate(ref Matrix4 input, out Matrix4 output);
		[AsmMethod("__MatrixInvert__unix","__MatrixInvert__windows","__MatrixInvert__cdecl")]
		public static MatrixInvertDelegate MatrixInvert;
		static byte[] __MatrixInvert__unix = new byte[] {
		0x0f,0x12,0x17,0x0f,0x12,0x6f,0x20,0x0f,0x16,0x57,0x10,0x0f,0x16,0x6f,0x30,0x0f,0x28,0xca,0x0f,0xc6,0xcd,0x88,0x0f,0xc6,0xea,0xdd,0x0f,0x12,0x57,0x08,0x0f,0x28,0xfd,0x0f,0x28,0xc2,0x0f,0x12,0x57,0x28,0x0f,0xc6,0xfd,0x4e,0x0f,0x16,0x47,0x18,0x0f,0x16,0x57,0x38,0x0f,0x28,0xf0,0x0f,0xc6,0xf2,0x88,0x44,0x0f,0x28,0xde,0x0f,0xc6,0xd0,0xdd,0x44,0x0f,0x28,0xc6,0x44,0x0f,0x59,0xda,0x0f,0xc6,0xf6,0x4e,0x44,0x0f,0x59,0xc5,0x0f,0x59,0xfa,0x45,0x0f,0xc6,0xdb,0xb1,0x41,0x0f,0x28,0xc3,0x41,0x0f,0x28,0xdb,0x41,0x0f,0xc6,0xc3,0x4e,0x0f,0x59,0xd9,0x0f,0x28,0xe0,0x45,0x0f,0xc6,0xc0,0xb1,0x0f,0x59,0xe1,0x45,0x0f,0x28,0xe0,0x0f,0xc6,0xff,0xb1,0x44,0x0f,0x28,0xcf,0x45,0x0f,0xc6,0xe0,0x4e,0x45,0x0f,0x28,0xd4,0x44,0x0f,0xc6,0xcf,0x4e,0x0f,0x59,0xc5,0x44,0x0f,0x59,0xd1,0x44,0x0f,0x59,0xdd,0x0f,0x5c,0xe3,0x44,0x0f,0x59,0xe2,0x41,0x0f,0x28,0xd8,0x0f,0x59,0xd9,0x44,0x0f,0x59,0xc2,0x0f,0xc6,0xe4,0x4e,0x44,0x0f,0x5c,0xd3,0x0f,0x28,0xd8,0x0f,0x28,0xc7,0x41,0x0f,0x5c,0xdb,0x0f,0x59,0xc6,0x0f,0x59,0xf9,0x45,0x0f,0xc6,0xd2,0x4e,0x41,0x0f,0x58,0xd8,0x44,0x0f,0x28,0xc2,0x44,0x0f,0x59,0xc1,0x41,0x0f,0x5c,0xdc,0x0f,0x58,0xd8,0x0f,0x28,0xc6,0x45,0x0f,0xc6,0xc0,0xb1,0x45,0x0f,0x28,0xe0,0x41,0x0f,0x59,0xc1,0x45,0x0f,0xc6,0xe0,0x4e,0x44,0x0f,0x59,0xc9,0x0f,0x5c,0xd8,0x0f,0x28,0xc5,0x44,0x0f,0x5c,0xcf,0x0f,0x59,0xc1,0x0f,0x28,0xfe,0x0f,0x59,0xf9,0x0f,0x59,0xcb,0x45,0x0f,0xc6,0xc9,0x4e,0x0f,0xc6,0xc0,0xb1,0x44,0x0f,0x28,0xe8,0x0f,0xc6,0xff,0xb1,0x44,0x0f,0x28,0xdf,0x44,0x0f,0xc6,0xe8,0x4e,0x44,0x0f,0x28,0xf1,0x44,0x0f,0xc6,0xdf,0x4e,0x44,0x0f,0xc6,0xf1,0x4e,0x41,0x0f,0x58,0xce,0x44,0x0f,0x28,0xf1,0x44,0x0f,0xc6,0xf1,0xb1,0xf3,0x44,0x0f,0x58,0xf1,0x41,0x0f,0x28,0xce,0xf3,0x41,0x0f,0x53,0xce,0x44,0x0f,0x28,0xf9,0xf3,0x44,0x0f,0x59,0xf9,0xf3,0x0f,0x58,0xc9,0xf3,0x45,0x0f,0x59,0xf7,0xf3,0x41,0x0f,0x5c,0xce,0x0f,0xc6,0xc9,0x00,0x0f,0x59,0xd9,0x0f,0x13,0x1e,0x0f,0x17,0x5e,0x08,0x0f,0x28,0xde,0x41,0x0f,0x59,0xd8,0x44,0x0f,0x59,0xc5,0x0f,0x5c,0xe3,0x0f,0x28,0xde,0x41,0x0f,0x59,0xdc,0x44,0x0f,0x59,0xe5,0x0f,0x58,0xe3,0x0f,0x28,0xda,0x0f,0x59,0xdf,0x0f,0x59,0xfd,0x41,0x0f,0x59,0xeb,0x0f,0x58,0xe3,0x0f,0x28,0xda,0x41,0x0f,0x59,0xdb,0x0f,0x5c,0xe3,0x0f,0x28,0xda,0x0f,0x59,0xd0,0x0f,0x59,0xc6,0x41,0x0f,0x59,0xdd,0x41,0x0f,0x59,0xf5,0x0f,0x59,0xe1,0x44,0x0f,0x58,0xca,0x41,0x0f,0x5c,0xc2,0x0f,0x28,0xd3,0x41,0x0f,0x5c,0xd1,0x0f,0x5c,0xc6,0x0f,0x13,0x66,0x10,0x0f,0x17,0x66,0x18,0x44,0x0f,0x58,0xc2,0x0f,0x5c,0xc7,0x45,0x0f,0x5c,0xc4,0x0f,0x58,0xc5,0x44,0x0f,0x59,0xc1,0x0f,0x59,0xc1,0x44,0x0f,0x13,0x46,0x20,0x44,0x0f,0x17,0x46,0x28,0x0f,0x13,0x46,0x30,0x0f,0x17,0x46,0x38,0xc3,
		};
		static byte[] __MatrixInvert__windows = new byte[] {
		0x48,0x81,0xec,0xa8,0x00,0x00,0x00,0x0f,0x12,0x11,0x0f,0x12,0x69,0x20,0x0f,0x29,0x34,0x24,0x0f,0x16,0x51,0x10,0x0f,0x16,0x69,0x30,0x44,0x0f,0x29,0x5c,0x24,0x50,0x0f,0x28,0xca,0x44,0x0f,0x29,0x44,0x24,0x20,0x0f,0xc6,0xcd,0x88,0x0f,0xc6,0xea,0xdd,0x0f,0x12,0x51,0x08,0x44,0x0f,0x29,0x64,0x24,0x60,0x0f,0x28,0xc2,0x0f,0x12,0x51,0x28,0x44,0x0f,0x29,0x54,0x24,0x40,0x0f,0x16,0x41,0x18,0x0f,0x29,0x7c,0x24,0x10,0x0f,0x28,0xfd,0x0f,0x16,0x51,0x38,0x0f,0x28,0xf0,0x0f,0xc6,0xfd,0x4e,0x44,0x0f,0x29,0x4c,0x24,0x30,0x0f,0xc6,0xf2,0x88,0x44,0x0f,0x28,0xde,0x0f,0xc6,0xd0,0xdd,0x44,0x0f,0x28,0xc6,0x44,0x0f,0x59,0xda,0x0f,0xc6,0xf6,0x4e,0x44,0x0f,0x29,0x6c,0x24,0x70,0x44,0x0f,0x29,0xb4,0x24,0x80,0x00,0x00,0x00,0x44,0x0f,0x59,0xc5,0x44,0x0f,0x29,0xbc,0x24,0x90,0x00,0x00,0x00,0x0f,0x59,0xfa,0x45,0x0f,0xc6,0xdb,0xb1,0x41,0x0f,0x28,0xc3,0x41,0x0f,0x28,0xdb,0x41,0x0f,0xc6,0xc3,0x4e,0x0f,0x59,0xd9,0x0f,0x28,0xe0,0x45,0x0f,0xc6,0xc0,0xb1,0x0f,0x59,0xe1,0x45,0x0f,0x28,0xe0,0x0f,0xc6,0xff,0xb1,0x44,0x0f,0x28,0xcf,0x45,0x0f,0xc6,0xe0,0x4e,0x45,0x0f,0x28,0xd4,0x44,0x0f,0xc6,0xcf,0x4e,0x0f,0x59,0xc5,0x44,0x0f,0x59,0xd1,0x44,0x0f,0x59,0xdd,0x0f,0x5c,0xe3,0x44,0x0f,0x59,0xe2,0x41,0x0f,0x28,0xd8,0x0f,0x59,0xd9,0x44,0x0f,0x59,0xc2,0x0f,0xc6,0xe4,0x4e,0x44,0x0f,0x5c,0xd3,0x0f,0x28,0xd8,0x0f,0x28,0xc7,0x41,0x0f,0x5c,0xdb,0x0f,0x59,0xc6,0x0f,0x59,0xf9,0x45,0x0f,0xc6,0xd2,0x4e,0x41,0x0f,0x58,0xd8,0x44,0x0f,0x28,0xc2,0x44,0x0f,0x59,0xc1,0x41,0x0f,0x5c,0xdc,0x0f,0x58,0xd8,0x0f,0x28,0xc6,0x45,0x0f,0xc6,0xc0,0xb1,0x45,0x0f,0x28,0xe0,0x41,0x0f,0x59,0xc1,0x45,0x0f,0xc6,0xe0,0x4e,0x44,0x0f,0x59,0xc9,0x0f,0x5c,0xd8,0x0f,0x28,0xc5,0x44,0x0f,0x5c,0xcf,0x0f,0x59,0xc1,0x0f,0x28,0xfe,0x0f,0x59,0xf9,0x0f,0x59,0xcb,0x45,0x0f,0xc6,0xc9,0x4e,0x0f,0xc6,0xc0,0xb1,0x44,0x0f,0x28,0xe8,0x0f,0xc6,0xff,0xb1,0x44,0x0f,0x28,0xdf,0x44,0x0f,0xc6,0xe8,0x4e,0x44,0x0f,0x28,0xf1,0x44,0x0f,0xc6,0xdf,0x4e,0x44,0x0f,0xc6,0xf1,0x4e,0x41,0x0f,0x58,0xce,0x44,0x0f,0x28,0xf1,0x44,0x0f,0xc6,0xf1,0xb1,0xf3,0x44,0x0f,0x58,0xf1,0x41,0x0f,0x28,0xce,0xf3,0x41,0x0f,0x53,0xce,0x44,0x0f,0x28,0xf9,0xf3,0x44,0x0f,0x59,0xf9,0xf3,0x0f,0x58,0xc9,0xf3,0x45,0x0f,0x59,0xf7,0xf3,0x41,0x0f,0x5c,0xce,0x0f,0xc6,0xc9,0x00,0x0f,0x59,0xd9,0x0f,0x13,0x1a,0x0f,0x17,0x5a,0x08,0x0f,0x28,0xde,0x41,0x0f,0x59,0xd8,0x44,0x0f,0x59,0xc5,0x0f,0x5c,0xe3,0x0f,0x28,0xde,0x41,0x0f,0x59,0xdc,0x44,0x0f,0x59,0xe5,0x0f,0x58,0xe3,0x0f,0x28,0xda,0x0f,0x59,0xdf,0x0f,0x59,0xfd,0x41,0x0f,0x59,0xeb,0x0f,0x58,0xe3,0x0f,0x28,0xda,0x41,0x0f,0x59,0xdb,0x0f,0x5c,0xe3,0x0f,0x28,0xda,0x0f,0x59,0xd0,0x0f,0x59,0xc6,0x41,0x0f,0x59,0xdd,0x41,0x0f,0x59,0xf5,0x0f,0x59,0xe1,0x44,0x0f,0x58,0xca,0x41,0x0f,0x5c,0xc2,0x44,0x0f,0x28,0x54,0x24,0x40,0x0f,0x28,0xd3,0x41,0x0f,0x5c,0xd1,0x44,0x0f,0x28,0x4c,0x24,0x30,0x0f,0x5c,0xc6,0x0f,0x13,0x62,0x10,0x0f,0x28,0x34,0x24,0x0f,0x17,0x62,0x18,0x44,0x0f,0x58,0xc2,0x0f,0x5c,0xc7,0x0f,0x28,0x7c,0x24,0x10,0x45,0x0f,0x5c,0xc4,0x0f,0x58,0xc5,0x44,0x0f,0x59,0xc1,0x0f,0x59,0xc1,0x44,0x0f,0x13,0x42,0x20,0x44,0x0f,0x17,0x42,0x28,0x44,0x0f,0x28,0x44,0x24,0x20,0x0f,0x13,0x42,0x30,0x0f,0x17,0x42,0x38,0x44,0x0f,0x28,0x5c,0x24,0x50,0x44,0x0f,0x28,0x64,0x24,0x60,0x44,0x0f,0x28,0xb4,0x24,0x80,0x00,0x00,0x00,0x44,0x0f,0x28,0x6c,0x24,0x70,0x44,0x0f,0x28,0xbc,0x24,0x90,0x00,0x00,0x00,0x48,0x81,0xc4,0xa8,0x00,0x00,0x00,0xc3,
		};
		static byte[] __MatrixInvert__cdecl = new byte[] {
		0x55,0x66,0x0f,0xef,0xd2,0x89,0xe5,0x83,0xe4,0xf0,0x83,0xc4,0x80,0x8b,0x55,0x08,0x8b,0x45,0x0c,0x0f,0x12,0x0a,0x0f,0x12,0x52,0x20,0x0f,0x16,0x52,0x30,0x0f,0x16,0x4a,0x10,0x0f,0x28,0xfa,0x0f,0x28,0xc1,0x0f,0xc6,0xf9,0xdd,0x0f,0x12,0x4a,0x08,0x0f,0xc6,0xc2,0x88,0x66,0x0f,0xef,0xd2,0x0f,0x16,0x4a,0x18,0x0f,0x12,0x52,0x28,0x0f,0x28,0xe9,0x0f,0x16,0x52,0x38,0x0f,0x29,0x7c,0x24,0x70,0x0f,0xc6,0xea,0x88,0x0f,0xc6,0xd1,0xdd,0x0f,0x29,0x54,0x24,0x60,0x0f,0x59,0xd5,0x0f,0xc6,0xd2,0xb1,0x0f,0x28,0xda,0x0f,0x28,0xe2,0x0f,0xc6,0xda,0x4e,0x0f,0x59,0xe0,0x0f,0x28,0xcb,0x0f,0x59,0xc8,0x0f,0x59,0x54,0x24,0x70,0x0f,0x59,0x5c,0x24,0x70,0x0f,0x5c,0xcc,0x0f,0x28,0xe5,0x0f,0xc6,0xed,0x4e,0x0f,0x29,0x6c,0x24,0x50,0x0f,0x59,0xe7,0x0f,0x5c,0xda,0x0f,0x28,0x54,0x24,0x60,0x0f,0xc6,0xc9,0x4e,0x0f,0x29,0x4c,0x24,0x10,0x0f,0x28,0xcc,0x0f,0xc6,0xcc,0xb1,0x0f,0x28,0xe1,0x0f,0x28,0xf9,0x0f,0xc6,0xe1,0x4e,0x0f,0x59,0xf8,0x0f,0x28,0xf4,0x0f,0x59,0xf0,0x0f,0x59,0xca,0x0f,0x59,0xe2,0x0f,0x28,0x54,0x24,0x60,0x0f,0x59,0xd0,0x0f,0x5c,0xf7,0x0f,0x58,0xd9,0x0f,0x28,0xfe,0x0f,0xc6,0xfe,0x4e,0x0f,0x28,0x74,0x24,0x70,0x0f,0x5c,0xdc,0x0f,0xc6,0xd2,0xb1,0x0f,0x28,0x64,0x24,0x50,0x0f,0xc6,0xf6,0x4e,0x0f,0x59,0x74,0x24,0x60,0x0f,0x28,0xcc,0x0f,0xc6,0xf6,0xb1,0x0f,0x59,0xce,0x0f,0x28,0xee,0x0f,0xc6,0xee,0x4e,0x0f,0x59,0xf0,0x0f,0x58,0xd9,0x0f,0x28,0xcc,0x0f,0x59,0xcd,0x0f,0x59,0xe8,0x0f,0x59,0xe0,0x0f,0x5c,0xd9,0x0f,0x28,0xca,0x0f,0x5c,0xee,0x0f,0x28,0x74,0x24,0x70,0x0f,0xc6,0xca,0x4e,0x0f,0x29,0x4c,0x24,0x30,0x0f,0xc6,0xe4,0xb1,0x0f,0x28,0xcc,0x0f,0x59,0xf0,0x0f,0xc6,0xcc,0x4e,0x0f,0x29,0x4c,0x24,0x20,0x0f,0x59,0xc3,0x0f,0xc6,0xed,0x4e,0x0f,0x29,0x2c,0x24,0x0f,0xc6,0xf6,0xb1,0x0f,0x28,0xee,0x0f,0x28,0xc8,0x0f,0xc6,0xee,0x4e,0x0f,0x29,0x6c,0x24,0x40,0x0f,0xc6,0xc8,0x4e,0x0f,0x58,0xc1,0x0f,0x28,0xc8,0x0f,0xc6,0xc8,0xb1,0xf3,0x0f,0x58,0xc8,0x0f,0x28,0xc1,0xf3,0x0f,0x53,0xc1,0x0f,0x28,0xe8,0xf3,0x0f,0x59,0xe8,0xf3,0x0f,0x58,0xc0,0xf3,0x0f,0x59,0xcd,0xf3,0x0f,0x5c,0xc1,0x0f,0xc6,0xc0,0x00,0x0f,0x59,0xd8,0x0f,0x13,0x18,0x0f,0x17,0x58,0x08,0x0f,0x28,0x6c,0x24,0x50,0x0f,0x28,0x5c,0x24,0x10,0x0f,0x28,0xcd,0x0f,0x59,0xca,0x0f,0x5c,0xd9,0x0f,0x28,0x4c,0x24,0x30,0x0f,0x59,0xcd,0x0f,0x28,0x6c,0x24,0x60,0x0f,0x58,0xd9,0x0f,0x28,0xcd,0x0f,0x59,0xcc,0x0f,0x58,0xcb,0x0f,0x28,0x5c,0x24,0x20,0x0f,0x59,0xdd,0x0f,0x5c,0xcb,0x0f,0x59,0xc8,0x0f,0x13,0x48,0x10,0x0f,0x17,0x48,0x18,0x0f,0x28,0x4c,0x24,0x40,0x0f,0x28,0x5c,0x24,0x70,0x0f,0x59,0xcd,0x0f,0x59,0xee,0x0f,0x58,0x2c,0x24,0x0f,0x59,0xd3,0x0f,0x5c,0xcd,0x0f,0x58,0xca,0x0f,0x28,0x54,0x24,0x30,0x0f,0x59,0xd3,0x0f,0x5c,0xca,0x0f,0x59,0xc8,0x0f,0x13,0x48,0x20,0x0f,0x17,0x48,0x28,0x0f,0x28,0x5c,0x24,0x50,0x0f,0x28,0x4c,0x24,0x20,0x0f,0x59,0xf3,0x0f,0x59,0x5c,0x24,0x40,0x0f,0x5c,0xf7,0x0f,0x28,0x7c,0x24,0x70,0x0f,0x59,0xe7,0x0f,0x5c,0xf3,0x0f,0x59,0xcf,0x0f,0x5c,0xf4,0x0f,0x58,0xf1,0x0f,0x59,0xc6,0x0f,0x13,0x40,0x30,0x0f,0x17,0x40,0x38,0xc9,0xc3,
		};
		[SuppressUnmanagedCodeSecurity]
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MatrixMultiplyDelegate(ref Matrix4 a, ref Matrix4 b, out Matrix4 output);
		[AsmMethod("__MatrixMultiply__unix","__MatrixMultiply__windows","__MatrixMultiply__cdecl")]
		public static MatrixMultiplyDelegate MatrixMultiply;
		static byte[] __MatrixMultiply__unix = new byte[] {
		0x0f,0x10,0x06,0x0f,0x10,0x4e,0x10,0x0f,0x10,0x56,0x20,0x0f,0x10,0x5e,0x30,0x48,0xc7,0xc0,0x30,0x00,0x00,0x00,0xf3,0x0f,0x10,0x24,0x07,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe0,0x0f,0x10,0xec,0xf3,0x0f,0x10,0x64,0x07,0x04,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe1,0x0f,0x58,0xec,0xf3,0x0f,0x10,0x64,0x07,0x08,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe2,0x0f,0x58,0xec,0xf3,0x0f,0x10,0x64,0x07,0x0c,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe3,0x0f,0x58,0xec,0x0f,0x11,0x2c,0x02,0x48,0x83,0xe8,0x10,0x7d,0xb7,0xc3,
		};
		static byte[] __MatrixMultiply__windows = new byte[] {
		0x0f,0x10,0x02,0x0f,0x10,0x4a,0x10,0x0f,0x10,0x52,0x20,0x0f,0x10,0x5a,0x30,0x48,0xc7,0xc0,0x30,0x00,0x00,0x00,0xf3,0x0f,0x10,0x24,0x01,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe0,0x0f,0x10,0xec,0xf3,0x0f,0x10,0x64,0x01,0x04,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe1,0x0f,0x58,0xec,0xf3,0x0f,0x10,0x64,0x01,0x08,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe2,0x0f,0x58,0xec,0xf3,0x0f,0x10,0x64,0x01,0x0c,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe3,0x0f,0x58,0xec,0x41,0x0f,0x11,0x2c,0x00,0x48,0x83,0xe8,0x10,0x7d,0xb6,0xc3,
		};
		static byte[] __MatrixMultiply__cdecl = new byte[] {
		0x53,0x8b,0x5c,0x24,0x08,0x8b,0x4c,0x24,0x0c,0x8b,0x54,0x24,0x10,0x0f,0x10,0x01,0x0f,0x10,0x49,0x10,0x0f,0x10,0x51,0x20,0x0f,0x10,0x59,0x30,0xb8,0x30,0x00,0x00,0x00,0xf3,0x0f,0x10,0x24,0x03,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe0,0x0f,0x10,0xec,0xf3,0x0f,0x10,0x64,0x03,0x04,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe1,0x0f,0x58,0xec,0xf3,0x0f,0x10,0x64,0x03,0x08,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe2,0x0f,0x58,0xec,0xf3,0x0f,0x10,0x64,0x03,0x0c,0x0f,0xc6,0xe4,0x00,0x0f,0x59,0xe3,0x0f,0x58,0xec,0x0f,0x11,0x2c,0x02,0x83,0xe8,0x10,0x7d,0xb8,0x5b,0xc3,
		};
	#pragma warning restore 414
	}
}

