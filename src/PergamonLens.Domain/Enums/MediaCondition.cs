namespace PergamonLens.Domain.Enums;

/// <summary>
/// Escala de graduación estándar Goldmine empleada internacionalmente por coleccionistas y Discogs.
/// </summary>
public enum MediaCondition
{
    Mint = 1,          // Impecable, nuevo a estrenar (a menudo precintado)
    NearMint = 2,      // Casi perfecto, sin marcas visibles ni deterioro acústico
    VeryGoodPlus = 3,  // Muy buen estado (VG+), signos leves de uso que no alteran la escucha
    VeryGood = 4,      // Buen estado con marcas superficiales y leve ruido de fondo ocasional
    GoodPlus = 5,      // Marcas evidentes, desgaste visible y ruido apreciable
    Good = 6,          // Desgaste pronunciado, reproduce sin saltos pero con chasquidos continuos
    Fair = 7,          // Daños graves, puede saltar en alguna pista
    Poor = 8           // Deteriorado, roto o injugable
}

