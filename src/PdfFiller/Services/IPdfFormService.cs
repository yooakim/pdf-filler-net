using PdfFiller.Models;

namespace PdfFiller.Services;

public interface IPdfFormService
{
    IReadOnlyList<PdfFieldInfo> GetFields(string pdfPath);
    void Fill(string inputPath, string outputPath, IReadOnlyList<PdfFieldInfo> fields);
}
