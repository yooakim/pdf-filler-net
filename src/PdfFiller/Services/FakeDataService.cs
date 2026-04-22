using Bogus;
using Microsoft.Extensions.Configuration;
using PdfFiller.Models;
using Serilog;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfFiller.Services;

public sealed class FakeDataService : IFakeDataService
{
    private readonly Faker _faker;
    private static readonly Random _rng = new();

    public FakeDataService(IConfiguration config)
    {
        var locale = config["PdfFiller:DefaultLocale"] ?? "sv";
        _faker = new Faker(locale);
        Log.Debug("FakeDataService initialised with locale {Locale}", locale);
    }

    public string GetValue(string fieldName, PdfFieldType fieldType, string[]? choiceOptions = null)
    {
        if (fieldType == PdfFieldType.Checkbox || fieldType == PdfFieldType.Radio)
            return _rng.NextDouble() > 0.5 ? "Yes" : "Off";

        if (fieldType == PdfFieldType.Choice)
        {
            if (choiceOptions is { Length: > 0 })
                return choiceOptions[_rng.Next(choiceOptions.Length)];
            return _faker.Lorem.Word();
        }

        if (fieldType == PdfFieldType.Signature)
            return string.Empty;

        return MapByName(Normalize(fieldName));
    }

    private string MapByName(string key)
    {
        if (Contains(key, "firstname", "förnamn", "fname", "givenname"))
            return _faker.Name.FirstName();
        if (Contains(key, "lastname", "efternamn", "lname", "surname", "familyname", "familjenamn"))
            return _faker.Name.LastName();
        if (Contains(key, "fullname", "fullnamn", "name", "namn") && !Contains(key, "firstname", "lastname", "förnamn", "efternamn", "company", "företag"))
            return _faker.Name.FullName();
        if (Contains(key, "email", "epost", "e-post", "mail"))
            return _faker.Internet.Email();
        if (Contains(key, "phone", "telefon", "tel", "mobile", "mobil", "cell"))
            return _faker.Phone.PhoneNumber();
        if (Contains(key, "address", "adress", "street", "gata", "gatuadress"))
            return _faker.Address.StreetAddress();
        if (Contains(key, "city", "stad", "ort", "postort"))
            return _faker.Address.City();
        if (Contains(key, "zip", "postal", "postnr", "postnummer", "postcode"))
            return _faker.Address.ZipCode();
        if (Contains(key, "country", "land"))
            return "Sverige";
        if (Contains(key, "state", "province", "lan", "län"))
            return _faker.Address.State();
        if (Contains(key, "company", "företag", "bolag", "org", "organisation", "organization", "employer", "arbetsgivare"))
            return _faker.Company.CompanyName();
        if (Contains(key, "jobtitle", "title", "titel", "position", "occupation", "yrke"))
            return _faker.Name.JobTitle();
        if (Contains(key, "website", "url", "web", "homepage", "hemsida"))
            return _faker.Internet.Url();
        if (Contains(key, "ssn", "personnummer", "pnr", "personalnumber", "civicnumber"))
            return GenerateSwedishPersonnummer();
        if (Contains(key, "age", "alder", "ålder"))
            return _rng.Next(18, 80).ToString();
        if (Contains(key, "birthdate", "birthday", "fodelsedatum", "födelsedag", "dob", "born"))
            return _faker.Date.Past(60, DateTime.Now.AddYears(-18)).ToString("yyyy-MM-dd");
        if (Contains(key, "date", "datum"))
            return _faker.Date.Past(1).ToString("yyyy-MM-dd");
        if (Contains(key, "amount", "belopp", "sum", "summa", "price", "pris"))
            return _rng.Next(100, 100000).ToString();
        if (Contains(key, "description", "beskrivning", "comment", "kommentar", "note", "anteckning", "message", "meddelande"))
            return _faker.Lorem.Sentence();
        if (Contains(key, "number", "nummer", "no", "nr", "id"))
            return _rng.Next(1000, 9999).ToString();

        Log.Debug("No pattern matched for field {Field}, using Lorem.Word()", key);
        return _faker.Lorem.Word();
    }

    private static string Normalize(string fieldName)
    {
        // Strip diacritics and lowercase for pattern matching, but keep Swedish chars as-is for direct comparison
        var sb = new StringBuilder(fieldName.Length);
        foreach (var c in fieldName.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-')
                sb.Append(c);
        }
        return sb.ToString();
    }

    private static bool Contains(string normalizedKey, params string[] patterns)
        => patterns.Any(p => normalizedKey.Contains(Normalize(p)));

    private string GenerateSwedishPersonnummer()
    {
        var dob = _faker.Date.Past(60, DateTime.Now.AddYears(-18));
        var serial = _rng.Next(1, 999);
        var birthPart = dob.ToString("yyyyMMdd");
        var serialPart = serial.ToString("D3");
        var checkDigit = ComputeLuhn(birthPart[2..] + serialPart);
        return $"{birthPart}-{serialPart}{checkDigit}";
    }

    private static int ComputeLuhn(string digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var d = digits[i] - '0';
            if (i % 2 == 0) d *= 2;
            sum += d > 9 ? d - 9 : d;
        }
        return (10 - sum % 10) % 10;
    }
}
