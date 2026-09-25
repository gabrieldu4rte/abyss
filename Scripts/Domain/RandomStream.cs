using System;

namespace Abyss.Domain;
internal sealed class RandomStream
{
    internal Random Generator { get; set; } = new();
}
