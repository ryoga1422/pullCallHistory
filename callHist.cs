using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pullCallHistory
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class _0
    {
        
        public string call_id { get; set; }
        public string phone_number { get; set; }
        public string call_type { get; set; }
        public string rule_id { get; set; }
        public string camp_id { get; set; }
        public string call_start_time { get; set; }
        public string call_duration { get; set; }
        public object rule_name { get; set; }
        public string tenant_id { get; set; }
        public string tenant_name { get; set; }
        public string access_number { get; set; }
        public string caller_number { get; set; }
        public string job_id { get; set; }
        public object job_name { get; set; }
        public string did_id { get; set; }
        public string trunk_id { get; set; }
        public string agt_id { get; set; }
        public string acd_id { get; set; }
        public string call_agent_time { get; set; }
        public string call_queue_time { get; set; }
        public string call_hangup_time { get; set; }
        public string disposition_id { get; set; }
        public string call_status { get; set; }
        public string hangup_reason { get; set; }
        public string hangup_by { get; set; }
        public string agent_duration { get; set; }
        public object wait_duration { get; set; }
        public object queue_name { get; set; }
        public string campaign_name { get; set; }
        public object trunk_name { get; set; }
        public object agent_username { get; set; }
        public object disposition_name { get; set; }
        public object extension_number { get; set; }
        public string sticky_agent_call { get; set; }
        public object callback_type { get; set; }
        public string transfer_id { get; set; }
        public object hold_duration { get; set; }
        public object mute_duration { get; set; }
        public string call_answer_time { get; set; }
        public string answred_duration { get; set; }
        public string cg_id { get; set; }
        public object disposition_duration { get; set; }
        public string customer_media_uuid { get; set; }
        public string customer_jitter_min_variance { get; set; }
        public string customer_jitter_max_variance { get; set; }
        public string customer_jitter_loss_rate { get; set; }
        public string customer_jitter_burst_rate { get; set; }
        public string customer_mean_interval { get; set; }
        public string customer_quality_percentage { get; set; }
        public string customer_mos { get; set; }
        public string customer_read_codec { get; set; }
        public string customer_write_codec { get; set; }
        public string customer_dtmf_type { get; set; }
        public string customer_local_media_ip { get; set; }
        public string customer_local_media_port { get; set; }
        public string customer_remote_media_ip { get; set; }
        public string customer_remote_media_port { get; set; }
        public string customer_user_agent { get; set; }
        public string customer_sip_call_id { get; set; }
        public object agent_media_uuid { get; set; }
        public object agent_jitter_min_variance { get; set; }
        public object agent_jitter_max_variance { get; set; }
        public object agent_jitter_loss_rate { get; set; }
        public object agent_jitter_burst_rate { get; set; }
        public object agent_mean_interval { get; set; }
        public object agent_quality_percentage { get; set; }
        public object agent_mos { get; set; }
        public object agent_read_codec { get; set; }
        public object agent_write_codec { get; set; }
        public object agent_dtmf_type { get; set; }
        public object agent_local_media_ip { get; set; }
        public object agent_local_media_port { get; set; }
        public object agent_remote_media_ip { get; set; }
        public object agent_remote_media_port { get; set; }
        public object agent_user_agent { get; set; }
        public object agent_sip_call_id { get; set; }
        public string recording_path { get; set; }
        public string customer_pcap { get; set; }
        public string agent_pcap { get; set; }
    }

    public class Result
    {
        [JsonProperty("id")]
        public Dictionary<string,_0> _0 { get; set; }
        public int total { get; set; }
    }

    public class RootCallHist
    {
        public int code { get; set; }
        public string status { get; set; }
        public string status_message { get; set; }
        public Result result { get; set; }
    }




}
