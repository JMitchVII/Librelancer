﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LibreLancer.Vertices;

namespace LibreLancer
{
    public class VertexBuffer : IDisposable
    {
		public static int TotalDrawcalls = 0;
        public static int TotalBuffers = 0;
        private bool isDisposed = false;
        public int VertexCount { get; private set; }
        uint VBO;
		uint VAO;
        bool streaming;
        public bool HasElements => _elements != null;
		Type type;
		VertexDeclaration decl;
		IVertexType vertextype;
		public IVertexType VertexType {
			get {
				return vertextype;
			}
		}
		ElementBuffer _elements;
		public ElementBuffer Elements
		{
			get
			{
				return _elements;
			}
		}
		public VertexBuffer(Type type, int length, bool isStream = false)
        {
            TotalBuffers++;
            VBO = GL.GenBuffer();
			var usageHint = isStream ? GL.GL_STREAM_DRAW : GL.GL_STATIC_DRAW;
            streaming = isStream;
            this.type = type;
            try
            {
				vertextype = (IVertexType)Activator.CreateInstance (type);
				decl = vertextype.GetVertexDeclaration();
            }
            catch (Exception)
            {
                throw new Exception(string.Format("{0} is not a valid IVertexType", type.FullName));
            }
			GL.GenVertexArrays (1, out VAO);
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
			GL.BufferData (GL.GL_ARRAY_BUFFER, (IntPtr)(length * decl.Stride), IntPtr.Zero, usageHint);
            if(isStream)
                buffer = Marshal.AllocHGlobal(length * decl.Stride);
			decl.SetPointers ();
			VertexCount = length;
        }

        public VertexBuffer(VertexDeclaration decl, int length, bool isStream = false)
        {
            this.decl = decl;
            VBO = GL.GenBuffer();
            streaming = isStream;
            var usageHint = isStream ? GL.GL_STREAM_DRAW : GL.GL_STATIC_DRAW;
            GL.GenVertexArrays(1, out VAO);
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            GL.BufferData(GL.GL_ARRAY_BUFFER, (IntPtr)(length * decl.Stride), IntPtr.Zero, usageHint);
            decl.SetPointers();
            VertexCount = length;
        }

		public void SetData<T>(T[] data, int? length = null, int? start = null) where T : struct
        {
            if (typeof(T) != type && typeof(T) != typeof(byte))
                throw new Exception("Data must be of type " + type.FullName);
			int len = length ?? data.Length;
            int s = start ?? 0;
			GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
			var handle = GCHandle.Alloc (data, GCHandleType.Pinned);
			GL.BufferSubData (GL.GL_ARRAY_BUFFER, (IntPtr)(s * decl.Stride), (IntPtr)(len * decl.Stride), handle.AddrOfPinnedObject());
			handle.Free ();
        }

        public void Expand(int newSize)
        {
            if (newSize < VertexCount)
                throw new InvalidOperationException();
            var newHandle = GL.GenBuffer();
            GL.BindBuffer(GL.GL_COPY_READ_BUFFER, VBO);
            GL.BindBuffer(GL.GL_COPY_WRITE_BUFFER, newHandle);
            GL.BufferData(GL.GL_COPY_WRITE_BUFFER, new IntPtr(newSize * decl.Stride), IntPtr.Zero, streaming ? GL.GL_STREAM_DRAW : GL.GL_STATIC_DRAW);
            GL.CopyBufferSubData(GL.GL_COPY_READ_BUFFER, GL.GL_COPY_WRITE_BUFFER, IntPtr.Zero, IntPtr.Zero, (IntPtr)(VertexCount * decl.Stride));
            GL.DeleteBuffer(VBO);
            VBO = newHandle;
            GLBind.VertexArray(VAO);
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            decl.SetPointers();
            VertexCount = newSize;
        }

		public void Draw(PrimitiveTypes primitiveType, int baseVertex, int startIndex, int primitiveCount)
		{
            if (primitiveCount == 0) throw new InvalidOperationException("primitiveCount can't be 0");
            if (!HasElements)
                throw new InvalidOperationException("Cannot use drawElementsBaseVertex without element buffer");
            RenderContext.Instance.Apply ();
			int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
			GLBind.VertexArray (VAO);
			GL.DrawElementsBaseVertex (primitiveType.GLType (),
				indexElementCount,
				GL.GL_UNSIGNED_SHORT,
				(IntPtr)(startIndex * 2),
				baseVertex);
			TotalDrawcalls++;
		}

        public unsafe void DrawImmediateElements(PrimitiveTypes primitiveTypes, int baseVertex, ReadOnlySpan<ushort> elements)
        {
            if (elements.Length == 0) throw new InvalidOperationException("elements length can't be 0");
            RenderContext.Instance.Apply();
            GLBind.VertexArray(VAO);
            var eb = GL.GenBuffer();
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, eb);
            fixed (ushort* ptr = &elements.GetPinnableReference())
                GL.BufferData(GL.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)(elements.Length * 2), (IntPtr)ptr, GL.GL_STREAM_DRAW);
            GL.DrawElementsBaseVertex(primitiveTypes.GLType(),elements.Length, GL.GL_UNSIGNED_SHORT, IntPtr.Zero, baseVertex);
            GL.DeleteBuffer(eb);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, _elements?.Handle ?? 0);
        }

        const int STREAM_FLAGS = GL.GL_MAP_WRITE_BIT | GL.GL_MAP_INVALIDATE_BUFFER_BIT;
        private IntPtr buffer;
        public IntPtr BeginStreaming()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(VertexBuffer));
            if (!streaming) throw new InvalidOperationException("not streaming buffer");
            return buffer;
        }

        //Count is for if emulation is required
        public void EndStreaming(int count)
        {
            if (!streaming) throw new InvalidOperationException("not streaming buffer");
            if (count == 0) return;
            GL.BindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            GL.BufferData(GL.GL_ARRAY_BUFFER, (IntPtr)(VertexCount * decl.Stride), IntPtr.Zero, GL.GL_STREAM_DRAW);
            GL.BufferSubData(GL.GL_ARRAY_BUFFER, IntPtr.Zero, (IntPtr) (count * decl.Stride), buffer);
        }

        internal void Bind()
        {
            GLBind.VertexArray(VAO);
        }

		public void Draw(PrimitiveTypes primitiveType, int primitiveCount)
		{
            if (isDisposed) throw new ObjectDisposedException(nameof(VertexBuffer));
            RenderContext.Instance.Apply ();
			DrawInternal(primitiveType, primitiveCount);
		}

        internal void DrawInternal(PrimitiveTypes primitiveType, int primitiveCount)
        {
            GLBind.VertexArray (VAO);
            if (HasElements) {
                GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, _elements.Handle);
                int indexElementCount = primitiveType.GetArrayLength (primitiveCount);
                GL.DrawElements (primitiveType.GLType (),
                    indexElementCount,
                    GL.GL_UNSIGNED_SHORT,
                    IntPtr.Zero
                );
            } else {
                int indexElementCount = primitiveType.GetArrayLength(primitiveCount);
                GL.DrawArrays (primitiveType.GLType (),
                    0,
                    indexElementCount
                );
            }
            TotalDrawcalls++;
        }
		public void Draw(PrimitiveTypes primitiveType,int start, int primitiveCount)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(VertexBuffer));
			RenderContext.Instance.Apply();
			GLBind.VertexArray(VAO);
			if (HasElements)
			{
				int indexElementCount = primitiveType.GetArrayLength(primitiveCount);
				GL.DrawElements(primitiveType.GLType(),
					indexElementCount,
					GL.GL_UNSIGNED_SHORT,
				    (IntPtr)(2 * start)
				);
			}
			else
			{
				int indexElementCount = primitiveType.GetArrayLength(primitiveCount);
				GL.DrawArrays(primitiveType.GLType(),
					start,
					indexElementCount
				);
			}
			TotalDrawcalls++;
		}

        public void SetElementBuffer(ElementBuffer elems)
        {
			GLBind.VertexArray (VAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, elems.Handle);
            _elements = elems;
            elems.VertexBuffers.Add(this);
        }

        internal void RefreshElementBuffer()
        {
            GLBind.VertexArray (VAO);
            GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, _elements.Handle);
        }

		public void UnsetElementBuffer()
		{
			GLBind.VertexArray(VAO);
			GL.BindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, 0);
            _elements.VertexBuffers.Remove(this);
			_elements = null;
		}

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            TotalBuffers--;
            if(streaming)
                Marshal.FreeHGlobal(buffer);
            GL.DeleteBuffer(VBO);
			GL.DeleteVertexArray (VAO);
            GLBind.VertexArray(0);
        }
    }
}
