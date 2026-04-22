using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using PdfFiller.Models;
using Serilog;

namespace PdfFiller.Services;

public sealed class PdfFormService : IPdfFormService
{
    private readonly IFakeDataService _fakeData;

    public PdfFormService(IFakeDataService fakeData) => _fakeData = fakeData;

    public IReadOnlyList<PdfFieldInfo> GetFields(string pdfPath)
    {
        Log.Debug("Reading fields from {Path}", pdfPath);
        using var reader = new PdfReader(pdfPath);
        using var pdf = new PdfDocument(reader);

        var form = PdfAcroForm.GetAcroForm(pdf, false);
        if (form is null)
        {
            Log.Information("No AcroForm found in {Path}", pdfPath);
            return [];
        }

        var result = new List<PdfFieldInfo>();
        foreach (var (name, field) in form.GetAllFormFields())
        {
            var (type, options) = ClassifyField(field);
            if (type == PdfFieldType.Signature)
            {
                Log.Debug("Skipping signature field {Name}", name);
                continue;
            }

            var value = _fakeData.GetValue(name, type, options);
            result.Add(new PdfFieldInfo(name, type, value, options));
            Log.Debug("Field {Name} ({Type}) → {Value}", name, type, value);
        }

        return result;
    }

    public void Fill(string inputPath, string outputPath, IReadOnlyList<PdfFieldInfo> fields)
    {
        Log.Debug("Filling {Count} fields in {Path}", fields.Count, inputPath);
        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(reader, writer);

        var form = PdfAcroForm.GetAcroForm(pdf, false);
        if (form is null)
        {
            Log.Warning("No AcroForm in {Path} — nothing written", inputPath);
            return;
        }

        form.SetGenerateAppearance(true);

        foreach (var info in fields)
        {
            var field = form.GetField(info.Name);
            if (field is null)
            {
                Log.Warning("Field {Name} not found during fill pass", info.Name);
                continue;
            }

            try
            {
                field.SetValue(info.GeneratedValue);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not set value for field {Name}", info.Name);
            }
        }
    }

    private static (PdfFieldType type, string[]? options) ClassifyField(PdfFormField field)
    {
        if (field is PdfSignatureFormField) return (PdfFieldType.Signature, null);
        if (field is PdfTextFormField) return (PdfFieldType.Text, null);
        if (field is PdfChoiceFormField choice) return (PdfFieldType.Choice, GetChoiceOptions(choice));
        if (field is PdfButtonFormField btn)
            return (btn.IsRadio() ? PdfFieldType.Radio : PdfFieldType.Checkbox, null);
        return (PdfFieldType.Unknown, null);
    }

    private static string[]? GetChoiceOptions(PdfChoiceFormField choice)
    {
        try
        {
            var opts = choice.GetOptions();
            if (opts is null) return null;
            var result = new List<string>();
            for (var i = 0; i < opts.Size(); i++)
                result.Add(opts.GetAsString(i)?.ToUnicodeString() ?? string.Empty);
            return result.Count > 0 ? result.ToArray() : null;
        }
        catch
        {
            return null;
        }
    }
}
