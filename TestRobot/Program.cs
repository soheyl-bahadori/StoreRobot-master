using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TestRobot;


//while (true)
//{
//    Console.Write("enter Domestic Price: ");
//    var domesticPrice = Convert.ToInt32(Console.ReadLine());

//    Console.Write("enter Commission percent: ");
//    var commissionPercent = Convert.ToDouble(Console.ReadLine());

//    Console.Write("enter minimum Commission: ");
//    var minCommision = Convert.ToInt32(Console.ReadLine());

//    Console.Write("enter maximum Commission: ");
//    var maxCommision = Convert.ToInt32(Console.ReadLine());

//    Console.WriteLine(DigiUtils.FindMinPrice(domesticPrice, commissionPercent, minCommision, maxCommision));
//}


while (true)
{
    //Console.Write("Enter column address: ");
    //var input = Console.ReadLine();
    //Console.WriteLine(GetColumnNumber(input));


    Console.Write("Enter column number: ");
    var input2 = Console.ReadLine();
    Console.WriteLine(GetColumnAddress(Convert.ToInt32(input2)));
}




string GetColumnAddress(int columnNumber)
{
    //columnNumber += 3;
    StringBuilder columnAddress = new StringBuilder();

    while (columnNumber > 0)
    {
        int remainder = (columnNumber - 1) % 26;
        char ch = (char)('A' + remainder);
        columnAddress.Insert(0, ch);

        columnNumber = (columnNumber - 1) / 26;
    }

    return columnAddress.ToString();
}

int GetColumnNumber(string columnAddress)
{
    int result = 0;
    int len = columnAddress.Length;

    for (int i = 0; i < len; i++)
    {
        char ch = columnAddress[i];
        result = result * 26 + (ch - 'a' + 1);
    }

    return result;
}