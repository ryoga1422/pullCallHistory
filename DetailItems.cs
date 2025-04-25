using System;

public class DetailItems
{
	private string call_id;
	private string disposition_name;
	private string disposition_desc;
	private string phone_number;
	private string agt_name;
	private string ext_number;
	private string camp_name;
	private string acd_name;
	private string call_start_time;
	private string call_agent_time;
	private string call_hangup_time;
	private string call_duration;

	public DetailItems()
	{
	}

    public string Call_id { get => call_id; set => call_id = value; }
    public string Disposition_name { get => disposition_name; set => disposition_name = value; }
    public string Disposition_desc { get => disposition_desc; set => disposition_desc = value; }
    public string Phone_number { get => phone_number; set => phone_number = value; }
    public string Agt_name { get => agt_name; set => agt_name = value; }
    public string Ext_number { get => ext_number; set => ext_number = value; }
    public string Camp_name { get => camp_name; set => camp_name = value; }
    public string Acd_name { get => acd_name; set => acd_name = value; }
    public string Call_start_time { get => call_start_time; set => call_start_time = value; }
    public string Call_agent_time { get => call_agent_time; set => call_agent_time = value; }
    public string Call_hangup_time { get => call_hangup_time; set => call_hangup_time = value; }
    public string Call_duration { get => call_duration; set => call_duration = value; }
}
