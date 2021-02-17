namespace Ryujinx.HLE.HOS.Tamper.Atmosphere.Operations
{
    class OpAdd<T> : IOperation where T : unmanaged
    {
        IOperand _destination;
        IOperand _lhs;
        IOperand _rhs;

        public OpAdd(IOperand destination, IOperand lhs, IOperand rhs)
        {
            _destination = destination;
            _lhs = lhs;
            _rhs = rhs;
        }

        public void Execute()
        {
            _destination.Set((T)((dynamic)_lhs.Get<T>() + (dynamic)_rhs.Get<T>()));
        }
    }
}
