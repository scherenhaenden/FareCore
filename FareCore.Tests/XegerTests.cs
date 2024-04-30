using System.Globalization;
using System.Text.RegularExpressions;

namespace FareCore.Tests;

public class XegerTests
{
    
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Normalize the domain
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                RegexOptions.None, TimeSpan.FromMilliseconds(200));

            // Examines the domain part of the email and normalizes it.
            string DomainMapper(Match match)
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();

                // Pull out and process domain name (throws ArgumentException on invalid)
                string domainName = idn.GetAscii(match.Groups[2].Value);

                return match.Groups[1].Value + domainName;
            }
        }
        catch (RegexMatchTimeoutException e)
        {
            return false;
        }
        catch (ArgumentException e)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    
    [Test]
    public void TestShouldPass_HighMediumLow_Strings()
    {

        List<string> values = new List<string>();
        
        // run 100 times
        for (int i = 0; i < 100; i++)
        {
            var pattern =  "^(High|Medium| Low)$";
            var xeger = new Xeger(pattern);
            var createdString = xeger.Generate();
            Assert.IsTrue(Regex.IsMatch(createdString, pattern));
            values.Add(createdString);
            
        }
        
        
        Assert.Pass();
    }
    
    [Test]
    public void TestShouldPass_IBAN1_Strings()
    {

        List<string> values = new List<string>();
        
        // run 100 times
        for (int i = 0; i < 100; i++)
        {
            var pattern =  "^[A-Z]{2}[0-9]{2}[A-Z0-9]{4}[0-9]{7}([A-Z0-9]?){0,16}$";
            var xeger = new Xeger(pattern);
            var createdString = xeger.Generate();
            Assert.IsTrue(Regex.IsMatch(createdString, pattern));
            values.Add(createdString);
            
        }
        
        
        Assert.Pass();
    }
    
    [Test]
    public void TestShouldPass_Email_Strings()
    {

        List<string> values = new List<string>();
        
        // run 100 times
        for (int i = 0; i < 100; i++)
        {
            var pattern =  @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
            var xeger = new Xeger(pattern);
            var createdString = xeger.Generate();
            Assert.IsTrue(Regex.IsMatch(createdString, pattern));
            Assert.IsTrue(IsValidEmail(createdString));
            values.Add(createdString);
            
        }
        
        
        Assert.Pass();
    }
}