﻿using System;
using System.Collections.Generic;

#nullable disable

namespace FC_NDIS.Models
{
    public partial class CustomerServiceLinesNewbak
    {
        public int? ServiceAgreementCustomerId { get; set; }
        public string ServiceAgreementId { get; set; }
        public string ServiceAgreementName { get; set; }
        public DateTime ServiceAgreementEndDate { get; set; }
        public int? ServiceAgreementStatus { get; set; }
        public string ServiceAgreementFundingManagement { get; set; }
        public string ServiceAgreementFundingType { get; set; }
        public string ServiceAgreementItemId { get; set; }
        public string ServiceAgreementItemName { get; set; }
        public float SupportCategoryAmount { get; set; }
        public float? SupportCategoryDelivered { get; set; }
        public float? FundsRemaining { get; set; }
        public int? ItemOverclaim { get; set; }
        public string SiteId { get; set; }
        public string SiteName { get; set; }
        public string SiteServiceProgramId { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string RateId { get; set; }
        public string RateName { get; set; }
        public string RateType { get; set; }
        public float? RateAmount { get; set; }
        public bool? AllowRateNegotiation { get; set; }
        public int CustomerServiceLineId { get; set; }
        public bool? Default { get; set; }
    }
}
