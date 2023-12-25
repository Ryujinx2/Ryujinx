using Ryujinx.Graphics.Shader.IntermediateRepresentation;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstTextureOperation : AstOperation
    {
        public SamplerType Type { get; }
        public TextureFormat Format { get; }
        public TextureFlags Flags { get; }

        public int Binding { get; }

        public AstTextureOperation(
            Instruction inst,
            SamplerType type,
            TextureFormat format,
            TextureFlags flags,
            int binding,
            int index,
            params IAstNode[] sources) : base(inst, StorageKind.None, false, index, sources, sources.Length)
        {
            Type = type;
            Format = format;
            Flags = flags;
            Binding = binding;
        }
    }
}
