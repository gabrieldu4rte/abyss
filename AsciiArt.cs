using Godot;
using System;
using System.Collections.Generic;

// Original illustrations are archived in ArtSources. Runtime art is plain ASCII only.
public static class AsciiArt
{
	public static readonly Dictionary<string,string> ToneMaps=new();
	static string Read(string name)
	{
		string path="res://Art/"+name+".txt";
		string text=FileAccess.GetFileAsString(path);
		if(string.IsNullOrWhiteSpace(text))throw new InvalidOperationException("Missing ASCII illustration: "+path);
		ToneMaps[text]=FileAccess.GetFileAsString("res://Art/"+name+".tone");
		return text;
	}
	public static readonly string[] Heroes={Read("warrior"),Read("mage"),Read("archer"),Read("rogue")};
	public static readonly string Merchant=Read("merchant");
	public static readonly string Rat=Read("rat"),Skeleton=Read("skeleton"),Goblin=Read("goblin"),Warden=Read("warden"),Unknown=Read("unknown");
	public static readonly string Tower=Read("tower"),Camp=Read("camp"),Globe=Read("globe"),Book=Read("book"),Grave=Read("grave"),Crown=Read("crown"),Torch=Read("torch");
	public static string Enemy(char glyph)=>glyph switch {'r'=>Rat,'s'=>Skeleton,'g'=>Goblin,'B'=>Warden,_=>Unknown};
}
