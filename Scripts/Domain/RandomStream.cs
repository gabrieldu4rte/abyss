using System;

namespace Abyss.Domain;
// One stream per expedition; every subsystem consumes this same sequence.
internal sealed class RandomStream
{
    internal Random Generator { get; set; } = new();
}
