namespace PdfFiller.Models;

public enum PdfFieldType
{
    Text,
    Checkbox,
    Radio,
    Choice,
    Signature,
    Unknown
}

public sealed record PdfFieldInfo(
    string Name,
    PdfFieldType Type,
    string GeneratedValue,
    string[]? ChoiceOptions = null);
