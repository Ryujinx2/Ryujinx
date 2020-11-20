﻿using System;

namespace Ryujinx.HLE.HOS.Services.Nv
{
    internal struct MemoryBlock : IComparable<MemoryBlock>
    {
        public ulong address { get; }
        public ulong size { get; }
        public ulong endAddress { get; }

        public MemoryBlock(ulong address, ulong size)
        {
            this.address = address;
            this.size = size;
            this.endAddress = address + size - 1UL;
        }

        public int CompareTo(MemoryBlock other)
        {
            return this.address.CompareTo(other.address);
        }

        public override string ToString()
        {
            return $" [{address} - ({size}) -> {endAddress}] ";
        }
    }
}
