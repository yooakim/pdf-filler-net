using PdfFiller.Models;

namespace PdfFiller.Services;

public interface IFakeDataService
{
    string GetValue(string fieldName, PdfFieldType fieldType, string[]? choiceOptions = null);
}
