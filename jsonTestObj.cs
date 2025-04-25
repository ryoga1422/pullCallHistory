using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testJsonBBD
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class CallMedia
    {
        public string customer_uuid { get; set; }
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
        public string customer_pcap { get; set; }
        public string agent_uuid { get; set; }
        public string agent_jitter_min_variance { get; set; }
        public string agent_jitter_max_variance { get; set; }
        public string agent_jitter_loss_rate { get; set; }
        public string agent_jitter_burst_rate { get; set; }
        public string agent_mean_interval { get; set; }
        public string agent_quality_percentage { get; set; }
        public string agent_mos { get; set; }
        public string agent_read_codec { get; set; }
        public string agent_write_codec { get; set; }
        public string agent_dtmf_type { get; set; }
        public string agent_local_media_ip { get; set; }
        public string agent_local_media_port { get; set; }
        public string agent_remote_media_ip { get; set; }
        public string agent_remote_media_port { get; set; }
        public string agent_user_agent { get; set; }
        public string agent_pcap { get; set; }
    }

    public class HoldDetail
    {
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string duration { get; set; }
    }

    public class MuteDetail
    {
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string duration { get; set; }
    }

    public class PauseDetail
    {
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string duration { get; set; }
    }

    public class Result
    {
        public string call_id { get; set; }
        public string phone_number { get; set; }
        public string call_type { get; set; }
        public string did_id { get; set; }
        public string trunk_id { get; set; }
        public string rule_id { get; set; }
        public string agt_id { get; set; }
        public string acd_id { get; set; }
        public string camp_id { get; set; }
        public string call_start_time { get; set; }
        public string call_queue_time { get; set; }
        public string call_agent_time { get; set; }
        public string call_hangup_time { get; set; }
        public string call_disposition_time { get; set; }
        public string disposition_id { get; set; }
        public string call_status { get; set; }
        public string hangup_reason { get; set; }
        public string hangup_by { get; set; }
        public string call_duration { get; set; }
        public string agent_duration { get; set; }
        public string queue_name { get; set; }
        public string campagin_name { get; set; }
        public object trunk_name { get; set; }
        public object rule_name { get; set; }
        public string agent_username { get; set; }
        public string cg_id { get; set; }
        public string cust_name { get; set; }
        public string disposition_name { get; set; }
        public string tenant_id { get; set; }
        public string tenant_name { get; set; }
        public string callback_type { get; set; }
        public string transfer_id { get; set; }
        public string geo_location { get; set; }
        public string call_answer_time { get; set; }
        public string answred_duration { get; set; }
        public string recording_path { get; set; }
        public CallMedia call_media { get; set; }
    }

    public class RootHistory
    {
        public int code { get; set; }
        public string status { get; set; }
        public string status_message { get; set; }
        public Result result { get; set; }
        public List<PauseDetail> pause_details { get; set; }
        public List<MuteDetail> mute_details { get; set; }
        public List<HoldDetail> hold_details { get; set; }
    }


}
