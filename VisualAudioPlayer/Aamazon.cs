using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Threading;
using System.Threading;

namespace VisualAudioPlayer
{
    public class Aamazon
    {
        private const string MY_AWS_ACCESS_KEY_ID = "0B5QM4HJ9M2PXFHG9602";
        private const string MY_AWS_SECRET_KEY = "V9iFwk5ZLCdtEkPiUx4jFEYCo/4Ujbx13uWOnhTB";
        private const string DESTINATION = "webservices.amazon.com";
        //private const string DESTINATION = "ecs.amazonaws.de";
        private const string AssociateTag = "accespro-20";

        private const string NAMESPACE = "http://" + DESTINATION + "/AWSECommerceService/2011-08-01";
        //private const string ITEM_ID = "0545010225";
        public static string ASINLookup(string sASIN)
        {
            SignedRequestHelper helper = new SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION);
            String requestUrl; // The helper supports two forms of requests - dictionary form and query string form.
            /*
             * Here is an ItemLookup example where the request is stored as a dictionary.
             */
            IDictionary<string, string> r1 = new Dictionary<string, String>();
            r1["Service"] = "AWSECommerceService";
            r1["AssociateTag"] = AssociateTag;
            r1["Version"] = "2011-08-01";
            r1["Operation"] = "ItemLookup";
            r1["ItemId"] = sASIN;
            r1["IdType"] = "ASIN";
            r1["Condition"] = "All";
            r1["ResponseGroup"] = "Small";

            ///* Random params for testing */
            //r1["AnUrl"] = "http://www.amazon.com/books";
            //r1["AnEmailAddress"] = "foobar@nowhere.com";
            //r1["AUnicodeString"] = "αβγδεٵٶٷٸٹٺチャーハン叉焼";
            //r1["Latin1Chars"] = "ĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳ";

            requestUrl = helper.Sign(r1);

            Task<string> title = FetchItem("Title", requestUrl);
            return title.Result;
        }
        public static string ItemSearch(string keyword)
        {
            SignedRequestHelper helper = new SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION);
            String requestUrl; // The helper supports two forms of requests - dictionary form and query string form.

            String requestString = "Service=AWSECommerceService"
                + "&Version=2011-08-01"
                + "&AssociateTag=" + AssociateTag
                + "&Operation=ItemSearch"
                + "&SearchIndex=Books"
                + "&ResponseGroup=Small"
                + "&Keywords=" + keyword
                ;
            requestUrl = helper.Sign(requestString);
            Task<string> title = FetchItem("Title", requestUrl);

            return title.Result;
        }
        public static string CartCreate(String[] Keywords)
        {
            SignedRequestHelper helper = new SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION);
            String requestUrl; // The helper supports two forms of requests - dictionary form and query string form.

            String cartCreateRequestString =
                "Service=AWSECommerceService"
                + "&Version=2011-08-01"
                + "&AssociateTag=" + AssociateTag
                + "&Operation=CartCreate"
                + "&Item.1.OfferListingId=Ho46Hryi78b4j6Qa4HdSDD0Jhan4MILFeRSa9mK%2B6ZTpeCBiw0mqMjOG7ZsrzvjqUdVqvwVp237ZWaoLqzY11w%3D%3D"
                + "&Item.1.Quantity=1"
                ;
            requestUrl = helper.Sign(cartCreateRequestString);
            Task<string> title = FetchItem("Title", requestUrl);

            return title.Result;
        }
        public static string LargeImage(string sArtist, string sTitle)
        {
            if (string.IsNullOrEmpty(sArtist) || string.IsNullOrEmpty(sTitle))
                return null;
            SignedRequestHelper helper = new SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION);
            String requestUrl; // The helper supports two forms of requests - dictionary form and query string form.
            String requestString;
            String url;
            requestString = "Service=AWSECommerceService"
                  + "&Version=2011-08-01"
                  + "&AssociateTag=" + AssociateTag
                  + "&Operation=ItemSearch"
                  + "&SearchIndex=Music"
                  + "&ResponseGroup=Large"
                  + "&Artist=" + sArtist
                  + "&Title=" + sTitle
                  ;
            requestUrl = helper.Sign(requestString);
            url = GetItem(requestUrl, "LargeImage", "URL");
            if (url == null)
            {
                requestString = "Service=AWSECommerceService"
                    + "&Version=2011-08-01"
                    + "&AssociateTag=" + AssociateTag
                    + "&Operation=ItemSearch"
                    + "&SearchIndex=Music"
                    + "&ResponseGroup=Large"
                    + "&Title=" + sTitle
                    ;
                requestUrl = helper.Sign(requestString);
                url = GetItem(requestUrl, "LargeImage", "URL");
            }
            return url;
        }
        private static string GetItem(string url, string sItemName, string sSubItemName = null)
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                XElement content = response.Content.ReadAsAsync<XElement>().Result;
                XNamespace ns = NAMESPACE;
                var isValidResults = content.Descendants(ns + "IsValid").AsParallel();

                foreach (var item in isValidResults)
                {
                    if (item.Value != "True")
                        return "Invalid Request";
                }
                var titleResults = content.Descendants(ns + sItemName).AsParallel();
                foreach (XElement item in titleResults)
                {
                    if (item.Name == ns + sItemName)
                    {
                        if (sSubItemName == null)
                            return item.Value;
                        foreach (XElement i in item.Elements())
                        {
                            if (i.Name.LocalName == sSubItemName)
                                return i.Value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Caught Exception: " + e.Message);
                System.Console.WriteLine("Stack Trace: " + e.StackTrace);
            }
            return null;
        }
        private static async Task<string> FetchItem(string sItemName, string url)
        {
            try
            {
                
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                XElement content = await response.Content.ReadAsAsync<XElement>();
                XNamespace ns = NAMESPACE;
                var isValidResults = content.Descendants(ns + "IsValid").AsParallel();

                foreach (var item in isValidResults)
                {
                    if (item.Value != "True")
                        return "Invalid Request";
                }
                var titleResults = content.Descendants(ns + sItemName).AsParallel();
                foreach (var item in titleResults)
                {
                    if (item.Name == ns + sItemName)
                        return item.Value; // We return only the first title for matching keyword, but there can be 10 matches found
                    // depending on the keyword, results can be quite fun.... :-)
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Caught Exception: " + e.Message);
                System.Console.WriteLine("Stack Trace: " + e.StackTrace);
            }
            return "Error";
        }
        private static async Task<string> FetchTitle(string url)
        {
            Boolean validRequest = false;

            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                XElement content = await response.Content.ReadAsAsync<XElement>();
                XNamespace ns = NAMESPACE;
                var isValidResults = content.Descendants(ns + "IsValid").AsParallel();

                foreach (var item in isValidResults)
                {
                    if (item.Value == "True")
                        validRequest = true;
                    else
                        return "Invalid Request";
                }

                if (validRequest == true)
                {
                    var titleResults = content.Descendants(ns + "Title").AsParallel();
                    foreach (var item in titleResults)
                    {
                        if (item.Name == ns + "Title")
                            return item.Value; // We return only the first title for matching keyword, but there can be 10 matches found
                        // depending on the keyword, results can be quite fun.... :-)
                    }
                }

            }
            catch (Exception e)
            {
                System.Console.WriteLine("Caught Exception: " + e.Message);
                System.Console.WriteLine("Stack Trace: " + e.StackTrace);
            }

            return "Error";
        }
    }
}
