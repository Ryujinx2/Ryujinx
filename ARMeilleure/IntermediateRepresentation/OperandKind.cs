namespace ARMeilleure.IntermediateRepresentation
{
    enum OperandKind
    {
        Constant,
        Label,
        LocalVariable,
        Memory,
        Register,
        RegisterNoRename,
        Undefined
    }
}