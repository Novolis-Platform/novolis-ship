namespace Novolis.Ship.Structure;

public sealed record NestItem(string PartId, float Width, float Height, List<(float X, float Y)> Profile);
