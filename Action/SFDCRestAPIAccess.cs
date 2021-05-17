﻿using FC_NDIS.ActionInterface;
using FC_NDIS.ApplicationIntegartionModels;
using FC_NDIS.DBAccess;
using FC_NDIS.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FC_NDIS.RestAPIModels;
using FC_NDIS.APIModels.BllingLines;

namespace FC_NDIS.Action
{
    public class SFDCRestAPIAccess : ISFDC
    {
        public const string LoginEndpoint = "https://test.salesforce.com/services/oauth2/token";
        public const string ApiEndpoint = "/services/data/v36.0/"; //Use your org's version number

        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthToken { get; set; }
        public string ServiceUrl { get; set; }
        public ConfigurationBuilder _configurationBuilder = null;
        readonly HttpClient Client;

        private readonly IntegrationAppSettings _integrationAppSettings;
        private static NLog.ILogger logger = LogManager.GetCurrentClassLogger();


        public SFDCRestAPIAccess(IntegrationAppSettings integrationAppSettings)
        {
            Client = new HttpClient();
            _configurationBuilder = new ConfigurationBuilder();
            this._integrationAppSettings = integrationAppSettings;
        }


        public bool IntegerateSfCustServiceLine()
        {
            bool result = false;
            DBAction dba = new DBAction(_integrationAppSettings);
            List<CustomerServiceLine> ltsCusline = new List<CustomerServiceLine>();


            Login();
            logger.Info("Scheduled Customer Service Line job triggered");
            var queryCustomer = @"SELECT Id
                                                    ,Name
                                                    ,enrtcr__Remaining__c
                                                    ,enrtcr__Item_Overclaim__c
                                                    ,enrtcr__Support_Contract__c
                                                    ,enrtcr__Support_Contract__r.Name
                                                    ,enrtcr__Support_Contract__r.enrtcr__End_Date__c
                                                    ,enrtcr__Support_Contract__r.enrtcr__Status__c
                                                    ,enrtcr__Support_Contract__r.enrtcr__Funding_Type__c
                                                    ,enrtcr__Support_Contract__r.enrtcr__Funding_Management__c
                                                    ,enrtcr__Support_Contract__r.enrtcr__Client__c
                                                    ,enrtcr__Support_Category__c
                                                    ,enrtcr__Category_Item__r.enrtcr__Support_Category_Amount__c
                                                    ,enrtcr__Category_Item__r.enrtcr__Delivered__c
                                                    ,enrtcr__Site__c
                                                    ,enrtcr__Site__r.Name
                                                    ,enrtcr__Site__r.enrtcr__Site_GL_Code__c
                                                    ,enrtcr__Service__c
                                                    ,enrtcr__Service__r.Name
                                                    ,enrtcr__Service__r.enrtcr__Travel_Service__c
                                                    ,enrtcr__Service__r.enrtcr__Transport_Service__c
                                                    ,enrtcr__Site_Service_Program__c
                                                FROM enrtcr__Support_Contract_Item__c
                                                WHERE (
                                                        ( enrtcr__Service__r.enrtcr__Allow_Non_Labour_Transport__c = true
                                                        AND enrtcr__Service__r.enrtcr__Transport_Service__c != null
                                                        AND (
                                                                (
                                                                enrtcr__Support_Contract__r.enrtcr__Funding_Type__c = 'NDIS'                                                               
                                                                )
                                                                OR enrtcr__Support_Contract__r.enrtcr__Funding_Type__c != 'NDIS'
                                                           )
                                                        )
                                                    OR
                                                        ( enrtcr__Service__r.enrtcr__Allow_Non_Labour_Travel__c = true
                                                        AND enrtcr__Service__r.enrtcr__Travel_Service__c != null
                                                        AND (
                                                                (
                                                                enrtcr__Support_Contract__r.enrtcr__Funding_Type__c = 'NDIS'                                                               
                                                                )
                                                                OR enrtcr__Support_Contract__r.enrtcr__Funding_Type__c != 'NDIS'
                                                           )
                                                        )
                                                    )
                                                ";
            var APIResponse = QueryRecord(Client, queryCustomer);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var rootObject = JsonConvert.DeserializeObject<FC_NDIS.APIModels.CustomerServiceLine.Root>(APIResponse, settings);


            if (rootObject != null)
            {
                if (rootObject.records.Count() > 0)
                {
                    for (var i = 0; i <= rootObject.records.Count - 1; i++)
                    {
                        CustomerServiceLine csl = new CustomerServiceLine();
                        var customerId = rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Client__c;
                        csl.ServiceAgreementCustomerId = dba.GetCustomerId(customerId);
                        csl.ServiceAgreementId = rootObject.records[i].enrtcr__Support_Contract__c; ;
                        csl.ServiceAgreementName = rootObject.records[i].enrtcr__Support_Contract__r.Name;
                        csl.ServiceAgreementEndDate = Convert.ToDateTime(rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__End_Date__c);

                        if (rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Status__c == "Current")
                            csl.ServiceAgreementStatus = (int)CustomerStatus.Current;
                        if (rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Status__c == "Expired")
                            csl.ServiceAgreementStatus = (int)CustomerStatus.Expired;
                        if (rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Status__c == "Rollover")
                            csl.ServiceAgreementStatus = (int)CustomerStatus.Rollover;
                        if (rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Status__c == "Cancelled")
                            csl.ServiceAgreementStatus = (int)CustomerStatus.Cancelled;
                        if (rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Status__c == "Quote Submitted")
                            csl.ServiceAgreementStatus = (int)CustomerStatus.QuoteSubmitted;
                        if (rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Status__c == "Client Declined")
                            csl.ServiceAgreementStatus = (int)CustomerStatus.ClientDeclined;

                        csl.ServiceAgreementFundingManagement = rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Funding_Management__c;
                        csl.ServiceAgreementFundingType = rootObject.records[i].enrtcr__Support_Contract__r.enrtcr__Funding_Type__c;
                        csl.ServiceAgreementItemId = rootObject.records[i].Id;
                        csl.ServiceAgreementItemName = rootObject.records[i].Name;
                        if (rootObject.records[i]?.enrtcr__Category_Item__r?.enrtcr__Support_Category_Amount__c == null)
                            csl.SupportCategoryAmount = 0;
                        else
                            csl.SupportCategoryAmount = (float)rootObject.records[i].enrtcr__Category_Item__r.enrtcr__Support_Category_Amount__c;
                        csl.SupportCategoryDelivered = (float?)rootObject.records[i]?.enrtcr__Category_Item__r.enrtcr__Delivered__c ?? 0;
                        csl.FundsRemaining = (float?)rootObject.records[i].enrtcr__Remaining__c;

                        if (rootObject.records[i].enrtcr__Item_Overclaim__c == "Allow")
                            csl.ItemOverclaim = (int)ItemOverClaim.Allow;
                        if (rootObject.records[i].enrtcr__Item_Overclaim__c == "Warn")
                            csl.ItemOverclaim = (int)ItemOverClaim.Warn;
                        if (rootObject.records[i].enrtcr__Item_Overclaim__c == "Prevent")
                            csl.ItemOverclaim = (int)ItemOverClaim.Prevent;


                        csl.SiteId = rootObject.records[i].enrtcr__Site__c;
                        csl.SiteName = rootObject.records[i].enrtcr__Site__r.Name;
                        csl.SiteGlcode = rootObject.records[i].enrtcr__Site__r.enrtcr__Site_GL_Code__c;
                        csl.SiteServiceProgramId = rootObject.records[i].enrtcr__Site_Service_Program__c;
                        csl.ServiceId = rootObject.records[i].enrtcr__Service__c;
                        csl.ServiceName = rootObject.records[i].enrtcr__Service__r.Name;
                        csl.TravelServiceId = rootObject.records[i].enrtcr__Service__r.enrtcr__Travel_Service__c;
                        csl.TransportServiceId = rootObject.records[i].enrtcr__Service__r.enrtcr__Transport_Service__c == null ? "" : rootObject.records[i].enrtcr__Service__r.enrtcr__Transport_Service__c;
                        csl.CategoryItemId = rootObject.records[i].enrtcr__Support_Category__c;
                        // csl.RateId = rootObject.records[i].enrtcr__Rate__c;
                        // csl.RateName = rootObject.records[i].enrtcr__Rate__r.Name;
                        //csl.RateAmount = (float?)rootObject.records[i].enrtcr__Rate__r.enrtcr__Amount_Ex_GST__c;
                        // csl.RateType = rootObject.records[i].enrtcr__Rate__r.enrtcr__Quantity_Type__c;
                        // csl.AllowRateNegotiation = Convert.ToBoolean(rootObject.records[i].enrtcr__Rate__r.enrtcr__Allow_Rate_Negotiation__c == null ? false : true);
                        csl.Default = false;
                        ltsCusline.Add(csl);
                    }
                    //Insert record to Database

                    dba.IntegrateCustomerLineinfointoDB(ltsCusline);
                }
                result = true;
            }


            return result;
        }

        public bool IntegerateSfTransportRate()
        {
            bool result = false;
            List<SalesforceRate> ltsTransportRate = new List<SalesforceRate>();
            logger.Info("Scheduled Travel Rate job triggered");
            DBAction dba = new DBAction(_integrationAppSettings);
            Login();


            var queryCustomer = @"SELECT Id
                                                    ,enrtcr__Effective_Date__c
                                                    ,enrtcr__End_Date__c
                                                    ,Name
                                                    ,enrtcr__Service__c
                                                    ,enrtcr__Allow_Rate_Negotiation__c
                                                    ,enrtcr__Amount_Ex_GST__c
                                                    ,enrtcr__Quantity_Type__c
                                                FROM enrtcr__Rate__c
                                                WHERE enrtcr__Effective_Date__c <= TODAY
                                                    AND enrtcr__End_Date__c >= TODAY
                                                    AND (
                                                            (Name LIKE '%WA%' AND enrtcr__Funding_Type__c = 'NDIS')
                                                            OR enrtcr__Funding_Type__c != 'NDIS'
                                                        )
                                                    AND enrtcr__Service__c IN (
                                                        SELECT enrtcr__Transport_Service__c
                                                        FROM enrtcr__Service__c
                                                        WHERE enrtcr__Transport_Service__c != null
                                                            AND enrtcr__Allow_Non_Labour_Transport__c = true
                                                    )
                                                ORDER BY enrtcr__Service__c, enrtcr__Effective_Date__c desc
                                                ";
            var APIResponse = QueryRecord(Client, queryCustomer);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var rootObject = JsonConvert.DeserializeObject<FC_NDIS.APIModels.Rate.Root>(APIResponse, settings);

            if (rootObject != null)
            {
                if (rootObject.records.Count > 0)
                {
                    for (var i = 0; i <= rootObject.records.Count - 1; i++)
                    {
                        SalesforceRate tr = new SalesforceRate();
                        tr.RateId = rootObject.records[i].Id;
                        tr.StartDate = Convert.ToDateTime(rootObject.records[i].enrtcr__Effective_Date__c);
                        tr.EndDate = Convert.ToDateTime(rootObject.records[i].enrtcr__End_Date__c);
                        tr.RateName = rootObject.records[i].Name;
                        tr.ServiceId = rootObject.records[i].enrtcr__Service__c;
                        tr.Negotiation = rootObject.records[i].enrtcr__Allow_Rate_Negotiation__c;
                        tr.Rate = (float)rootObject.records[i].enrtcr__Amount_Ex_GST__c;
                        tr.RateType = 1;

                        tr.IsDeleted = false;
                        tr.CreatedDate = DateTime.Now;
                        tr.ModifiedDate = DateTime.Now;
                        ltsTransportRate.Add(tr);
                    }
                }
                //Insert record to Database
                dba.IntegrateTravelandTransportRateInfotoDB(ltsTransportRate);
            }

            return result;
        }

        public bool IntegerateSfTravelRate()
        {
            bool result = false;
            List<SalesforceRate> ltsTransportRate = new List<SalesforceRate>();
            logger.Info("Scheduled Travel Rate job triggered");
            DBAction dba = new DBAction(_integrationAppSettings);
            Login();


            //var queryCustomer = @"SELECT Id
            //                                        ,enrtcr__Effective_Date__c
            //                                        ,enrtcr__End_Date__c
            //                                        ,Name
            //                                        ,enrtcr__Service__c
            //                                        ,enrtcr__Allow_Rate_Negotiation__c
            //                                        ,enrtcr__Amount_Ex_GST__c
            //                                        ,enrtcr__Quantity_Type__c
            //                                    FROM enrtcr__Rate__c
            //                                    WHERE enrtcr__Effective_Date__c <= TODAY
            //                                        AND enrtcr__End_Date__c >= TODAY
            //                                        AND (
            //                                            (Name LIKE '%WA%' AND enrtcr__Funding_Type__c = 'NDIS')
            //                                            OR enrtcr__Funding_Type__c != 'NDIS'
            //                                        )
            //                                        AND enrtcr__Service__c IN (
            //                                            SELECT enrtcr__Travel_Service__c
            //                                            FROM enrtcr__Service__c
            //                                            WHERE enrtcr__Travel_Service__c != null
            //                                                AND enrtcr__Allow_Non_Labour_Travel__c = true
            //                                        )
            //                                    ORDER BY enrtcr__Service__c, enrtcr__Effective_Date__c desc
            //                                    ";
            var queryCustomer = @"SELECT Id,enrtcr__Effective_Date__c,enrtcr__End_Date__c,Name,enrtcr__Service__c,enrtcr__Allow_Rate_Negotiation__c,enrtcr__Amount_Ex_GST__c,enrtcr__Quantity_Type__c FROM enrtcr__Rate__c ";
            var APIResponse = QueryRecord(Client, queryCustomer);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var rootObject = JsonConvert.DeserializeObject<FC_NDIS.APIModels.Rate.Root>(APIResponse, settings);

            if (rootObject != null)
            {
                if (rootObject.records.Count > 0)
                {
                    for (var i = 0; i <= rootObject.records.Count - 1; i++)
                    {
                        SalesforceRate tr = new SalesforceRate();
                        tr.RateId = rootObject.records[i].Id;
                        tr.StartDate = Convert.ToDateTime(rootObject.records[i].enrtcr__Effective_Date__c);
                        tr.EndDate = Convert.ToDateTime(rootObject.records[i].enrtcr__End_Date__c);
                        tr.RateName = rootObject.records[i].Name;
                        tr.ServiceId = rootObject.records[i].enrtcr__Service__c;
                        tr.Negotiation = rootObject.records[i].enrtcr__Allow_Rate_Negotiation__c;
                        tr.Rate = (float)rootObject.records[i].enrtcr__Amount_Ex_GST__c;
                        tr.RateType = 1;

                        tr.IsDeleted = false;
                        tr.CreatedDate = DateTime.Now;
                        tr.ModifiedDate = DateTime.Now;
                        ltsTransportRate.Add(tr);
                    }
                }
                //Insert record to Database
                dba.IntegrateTravelandTransportRateInfotoDB(ltsTransportRate);
            }
            return result;
        }
        public bool IntegerateSfCustomeList()
        {
            Login();
            logger.Info("Integrate Customer Informations");
            var result = true;
            var queryCustomer = @"SELECT Id,Name,OtherStreet,OtherCity,OtherState,OtherPostalCode,RecordType.Name,Enrite_Care_Auto_Number__c,enrtcr__Status__c,LastModifiedDate FROM Contact WHERE RecordType.Name = 'Client' 
                       AND (enrtcr__Status__c='Current' OR enrtcr__Status__c='Deceased' OR enrtcr__Status__c='Inactive')";
            var APIResponse = QueryRecord(Client, queryCustomer);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var rootObject = JsonConvert.DeserializeObject<FC_NDIS.RestAPIModels.Customer.Root>(APIResponse, settings);

            List<Customer> lstCus = new List<Customer>();
            if (rootObject != null)
            {
                if (rootObject.records.Count > 0)
                {
                    for (var i = 0; i <= rootObject.records.Count - 1; i++)
                    {
                        Customer cs = new Customer();
                        cs.CustomerId = rootObject.records[i].Id;
                        cs.Name = rootObject.records[i].Name;
                        cs.Street = rootObject.records[i].OtherStreet;
                        cs.City = rootObject.records[i].OtherCity;
                        cs.State = rootObject.records[i].OtherState;
                        cs.PostalCode = rootObject.records[i].OtherPostalCode;
                        cs.LumaryId = rootObject.records[i].Enrite_Care_Auto_Number__c;
                        if (rootObject.records[i].enrtcr__Status__c != null)
                        {
                            cs.Active = false;
                            if (rootObject.records[i].enrtcr__Status__c == "Current")
                            {
                                cs.Status = 1;
                            }
                            if (rootObject.records[i].enrtcr__Status__c == "Deceased")
                            {
                                cs.Status = 2;
                            }
                            if (rootObject.records[i].enrtcr__Status__c == "Inactive")
                            {
                                cs.Status = 3;
                            }

                        }
                        cs.Active = true;
                        cs.OnHold = false;
                        lstCus.Add(cs);
                    }
                }
                DBAction dba = new DBAction(_integrationAppSettings);
                dba.IntegrateCustomerInfotoDB(lstCus);
            }
            return result;
        }
        private string QueryRecord(HttpClient client, string queryMessage)
        {
            string restQuery = $"{ServiceUrl}{_integrationAppSettings.SFDCApiEndpoint}query?q={queryMessage}";            
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthToken);
            HttpResponseMessage response = client.GetAsync(restQuery).Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        private string CreateRecord(HttpClient client, string createMessage, string recordType)
        {
            HttpContent contentCreate = new StringContent(createMessage, Encoding.UTF8, "application/xml");
            string uri = $"{ServiceUrl}{_integrationAppSettings.SFDCApiEndpoint}sobjects/{recordType}";

            HttpRequestMessage requestCreate = new HttpRequestMessage(HttpMethod.Post, uri);
            requestCreate.Headers.Add("Authorization", "Bearer Token " + AuthToken);
            requestCreate.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            requestCreate.Content = contentCreate;

            HttpResponseMessage response = client.SendAsync(requestCreate).Result;
            return response.Content.ReadAsStringAsync().Result;
        }


        public bool Login()
        {
            logger.Info("Login Method Triggered");

            try
            {
                HttpContent content = new FormUrlEncodedContent(new Dictionary<string, string>
                  {
                      {"grant_type", "password"},
                      {"client_id", _integrationAppSettings.SFDCClientId},
                      {"client_secret", _integrationAppSettings.SFDCClientSecret},
                      {"username",  _integrationAppSettings.SFDCUserName},
                      {"password", _integrationAppSettings.SFDCUserPassword}
                  });

                HttpResponseMessage message = Client.PostAsync(_integrationAppSettings.SFDCLoginEndpoint, content).Result;

                string response = message.Content.ReadAsStringAsync().Result;
                JObject obj = JObject.Parse(response);

                AuthToken = (string)obj["access_token"];
                ServiceUrl = (string)obj["instance_url"];

                logger.Info("Login Method successfully completed");
                return true;
            }
            catch (Exception ex)
            {
                logger.Info("Issue occured in the Login Method .Issue:" + ex.Message.ToString());
                return false;
            }
        }

        public bool IntegrateSFDCId_OperatortoDB(string Usernames)
        {
            bool result = false;
            logger.Info("Scheduled Driver job triggered");
            Login();
           

            var queryCustomer = @"Select Id,EmployeeNumber,UserRoleID,IsActive,Username,CompanyName From User WHERE Username IN (" + Usernames + ")";
            HttpClient cl = new HttpClient();
            cl.Timeout = new TimeSpan(0, 5, 0);
            var APIResponse = QueryRecord(cl, queryCustomer);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var rootObject = JsonConvert.DeserializeObject<FC_NDIS.RestAPIModels.Customer.Root>(APIResponse, settings);

            List<Customer> lstCus = new List<Customer>();
            if (rootObject != null)
            {
                if (rootObject.records.Count > 0)
                {
                    for (var i = 0; i <= rootObject.records.Count - 1; i++)
                    {
                        Customer cs = new Customer();
                        cs.CustomerId = rootObject.records[i].Id;
                        cs.Name = rootObject.records[i].Name;
                        cs.Street = rootObject.records[i].OtherStreet;
                        cs.City = rootObject.records[i].OtherCity;
                        cs.State = rootObject.records[i].OtherState;
                        cs.PostalCode = rootObject.records[i].OtherPostalCode;
                        cs.LumaryId = rootObject.records[i].Enrite_Care_Auto_Number__c;
                        if (rootObject.records[i].enrtcr__Status__c != null)
                        {
                            cs.Active = false;
                            if (rootObject.records[i].enrtcr__Status__c == "Current")
                            {
                                cs.Status = 1;
                            }
                            if (rootObject.records[i].enrtcr__Status__c == "Deceased")
                            {
                                cs.Status = 2;
                            }
                            if (rootObject.records[i].enrtcr__Status__c == "Inactive")
                            {
                                cs.Status = 3;
                            }

                        }
                        cs.Active = true;
                        cs.OnHold = false;
                        lstCus.Add(cs);
                    }
                    DBAction dba = new DBAction(_integrationAppSettings);
                    dba.IntegrateCustomerInfotoDB(lstCus);
                    return true;
                }
                else
                    return false;
                
            }


            return result;
        }

        public bool InsertDataintoSFDC()
        {
            bool Result = false;
            var bllist = GetBillingInformation();
            logger.Info("Insert Data into SFDC");
            Login();
            List<Customer> lstCus = new List<Customer>();
            List<int> ErrorCount = new List<int>();
            int count = 0;
            foreach (var bl in bllist)
            {
                count++;
                APIModels.BllingLines.enrtcr__Support_Delivered__c inputObj = new enrtcr__Support_Delivered__c();
                inputObj.Batch_Created__c = true;
                inputObj.enrtcr__Client__c = bl.enrtcr__Client__c;
                inputObj.enrtcr__Date__c = bl.enrtcr__Date__c;
                inputObj.enrtcr__Quantity__c = bl.enrtcr__Quantity__c;
                inputObj.enrtcr__Support_Contract_Item__c = bl.enrtcr__Support_Contract_Item__c;
                inputObj.enrtcr__Support_Contract__c = bl.enrtcr__Support_Contract__c;
                inputObj.enrtcr__Site__c = bl.enrtcr__Site__c;
                inputObj.enrtcr__Support_CategoryId__c = bl.enrtcr__Support_CategoryId__c;
                inputObj.enrtcr__Site_Service_Program__c = bl.enrtcr__Site_Service_Program__c;
                inputObj.enrtcr__Rate__c =bl.enrtcr__Rate__c;
                inputObj.enrtcr__Worker__c = bl.enrtcr__Worker__c;
                inputObj.enrtcr__Client_Rep_Accepted__c = true;
                inputObj.enrtcr__Use_Negotiated_Rate__c = true;
                inputObj.enrtcr__Negotiated_Rate_Ex_GST__c = bl.enrtcr__Negotiated_Rate_Ex_GST__c;
                inputObj.enrtcr__Negotiated_Rate_GST__c = bl.enrtcr__Negotiated_Rate_GST__c;
                try
                {
                    var json = JsonConvert.SerializeObject(inputObj);
                    var response = CreateRecord(Client, json, "enrtcr__Support_Delivered__c");
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    var rootObject = JsonConvert.DeserializeObject<FC_NDIS.APIModels.AccessResult.Root>(response, settings);
                    if (rootObject.success)
                    {
                        Result = true;
                    }
                    else
                        Result = false;

                }
                catch (Exception ex)
                {
                    ErrorCount.Add(count);
                    Result = false;
                }
            }

            // APIModels.BllingLines.enrtcr__Support_Delivered__c esdc = new enrtcr__Support_Delivered__c();
            //esdc.Batch_Created__c = true;
            //esdc.enrtcr__Client__c = "0035P000003ws2OQAQ";
            //esdc.enrtcr__Date__c = "2021-03-30";
            //esdc.enrtcr__Quantity__c = 10;
            //esdc.enrtcr__Support_Contract_Item__c = "a0n5P000000kHNgQAM";
            //esdc.enrtcr__Support_Contract__c = "a0o5P000000Bc9vQAC";
            //esdc.enrtcr__Site__c = "a0l5P000000046nQAA";
            //esdc.enrtcr__Support_CategoryId__c = "a0c5P000000Co8EQAS";
            //esdc.enrtcr__Site_Service_Program__c = "a0j5P000000dLBpQAM";
            //esdc.enrtcr__Rate__c = "a0b5P0000014SHkQAM";
            //esdc.enrtcr__Worker__c = "0057F000005AtbWQAS";
            //esdc.enrtcr__Client_Rep_Accepted__c = true;
            //esdc.enrtcr__Use_Negotiated_Rate__c = true;
            //esdc.enrtcr__Negotiated_Rate_Ex_GST__c =Convert.ToDecimal(0.85);
            //esdc.enrtcr__Negotiated_Rate_GST__c =Convert.ToDecimal(0.00);
            //try
            //{
            //    var json = JsonConvert.SerializeObject(esdc);
            //    var response = CreateRecord(Client, json, "enrtcr__Support_Delivered__c");
            //    Result = true;
            //}
            //catch(Exception ex)
            //{
            //    Result = false;
            //}
            return Result; ;
        }

        public List<FC_NDIS.JsonModels.SFDCBillingLines> GetBillingInformation()
        {
            DBAction dba = new DBAction(_integrationAppSettings);
            var res = dba.GetBillingInformation();
            return res;
        }


        public List<string> GetAllDriverInfo_NotMappedSFDC()
        {
            DBAction dba = new DBAction(_integrationAppSettings);
            return dba.GetDriverInformationIsnotMappedSFDC();
        }

        public enum ItemOverClaim
        {
            Allow = 1, Warn = 2, Prevent = 3
        }

        public enum CustomerStatus
        {
            Current = 1,
            Expired = 2,
            Rollover = 3,
            Cancelled = 4,
            QuoteSubmitted = 5,
            ClientDeclined = 6
        }
        public enum ItemServiceAgreement
        {
            Current = 1, Expired = 2
        }
    }
}