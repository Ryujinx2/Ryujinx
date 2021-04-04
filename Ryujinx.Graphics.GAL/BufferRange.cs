namespace Ryujinx.Graphics.GAL
{
    public readonly struct BufferRange
    {
        private static readonly BufferRange _empty = new BufferRange(BufferHandle.Null, 0, 0);

        public static ref readonly BufferRange Empty => ref _empty;

        public BufferHandle Handle { get; }

        public int Offset { get; }
        public int Size   { get; }

        public BufferRange(BufferHandle handle, int offset, int size)
        {
            Handle = handle;
            Offset = offset;
            Size   = size;
        }
    }
}