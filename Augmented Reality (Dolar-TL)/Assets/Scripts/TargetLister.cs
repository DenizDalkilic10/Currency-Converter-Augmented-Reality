using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using UnityEngine.Networking;
using System;
using System.Xml;


/**
    * The Controller Class which accesses and retrieves currency rate information from floatrates.com
    * and displays it on the canvas according to the active Image Target's properties 
 */

public class TargetLister : MonoBehaviour
{
   
    //Static Variables                          
    public static double total = 0;        // Total amount of money 
    public static string currency = "try"; //initialized with Turkish Lira

    //Public Variables (Initialized by the editor)
    public Text text_equivalent, exchangeRate, text_total;
    public GameObject up, down, set, reset, panelRate, panelTotal, panelResult;

    //Private Variables
    private enum InfoType { Rate, Description };
    private string xml_file;
    private int rateIndex;
    private string currencyTemp;            //used for checking whether the currency is changed or not
    private InfoType infoType;
    private List<CountryNode> countryNodes; //a list that holds countries and their information

    //inner class for country information
    public class CountryNode 
    {
        public string countryName;
        public string description;
        public double exchangeRate;
        public double inverseRate;

        public CountryNode(string name, double rate, string invDescription, double invRate)
        {
            countryName = name;
            description = invDescription;
            exchangeRate = rate;
            inverseRate = invRate;
        }
    }
    
    // Methods

    void Start()
    {
        rateIndex = 0;
        currencyTemp = currency;
        infoType = InfoType.Rate;
        countryNodes = new List<CountryNode>();
        StartCoroutine(GetText());
    }

    void Update()
    {
        if (currency != currencyTemp) //if the new currency is different from the old one
        {
            currencyTemp = currency;
            rateIndex = 0;
            StartCoroutine(GetText());
        }

        StateManager sm = TrackerManager.Instance.GetStateManager();
        IList<TrackableBehaviour> tbs = (IList<TrackableBehaviour>)sm.GetActiveTrackableBehaviours();

        up.SetActive(tbs.Count != 0);
        down.SetActive(tbs.Count != 0);
        set.SetActive(tbs.Count != 0);
        reset.SetActive(tbs.Count != 0);
        panelRate.SetActive(tbs.Count != 0);
        panelResult.SetActive(tbs.Count != 0);
        panelTotal.SetActive(tbs.Count != 0);
        text_equivalent.text = "";
        exchangeRate.text = "";
        text_total.text = "";
       
        foreach (TrackableBehaviour tb in tbs)
        {
            //Value of Total is updated by TargetImages (DefaultTrackableEventHandler Script)
            text_equivalent.text = (string)countryNodes[rateIndex].countryName + ": " + Math.Round(total / (double)countryNodes[rateIndex].inverseRate, 3);

            text_total.text = "Total: " + total;

            if (infoType == InfoType.Rate)
            {
                exchangeRate.fontSize = 14;
                exchangeRate.text = "Rate: " + countryNodes[rateIndex].inverseRate;
                //Debug.Log(countryNodes[rateIndex].inverseRate);
            }
            else if (infoType == InfoType.Description)
            {
                exchangeRate.fontSize = 10;
                exchangeRate.text = "" + countryNodes[rateIndex].description;
            }
        }
    }

    IEnumerator GetText()
    {
        UnityWebRequest www = UnityWebRequest.Get("http://www.floatrates.com/daily/"+currency+".xml");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            //Assign the xml to a string
            xml_file = www.downloadHandler.text;
            
            // Show results as text
            Debug.Log(www.downloadHandler.text);
           
            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;

            countryInfoExtractor(xml_file);
          
        }
        StopCoroutine(GetText());
    }

   
    private void countryInfoExtractor(string xmlFile) { //xml parser for retrieving country info
        countryNodes = new List<CountryNode>(); //old values disappear or kept in stack ?
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(xmlFile); 

        XmlNodeList xnList = xml.SelectNodes("/channel/item");
   
        foreach (XmlNode xn in xnList)
        {
            string targetName = xn["targetName"].InnerText;
            string description = xn["inverseDescription"].InnerText;
            double exchangeRate = Math.Round(Convert.ToDouble(xn["exchangeRate"].InnerText),3);
            double inverseRate = Math.Round(Convert.ToDouble(xn["inverseRate"].InnerText),3);
            countryNodes.Add(new CountryNode(targetName, exchangeRate, description, inverseRate));
        }
    }

    public void increaseRateIndex() {
        if (rateIndex == countryNodes.Count-1)
            rateIndex = 0;
        else
            rateIndex++;
    }

    public void decreaseRateIndex()
    {
        if (rateIndex == 0)
            rateIndex = countryNodes.Count-1;
        else
            rateIndex--;
    }

    public void resetTotal() {
        total = 0;
    }

    public void infoTypeToggle() {
        if (infoType == InfoType.Rate)
            infoType = InfoType.Description;
        else
            infoType = InfoType.Rate;
    }
}
