using System;
using System.Collections.Generic;

public class DispositionDetails
{
    #region Members
    private int code;
    private string status;
    private string status_message;
    private List<DetailItems> result;
    #endregion
    public DispositionDetails()
	{
	}
    public int getRows()
    {
        return result.Count;
    }
    public int Code { get => code; set => code = value; }
    public string Status { get => status; set => status = value; }
    public string Status_message { get => status_message; set => status_message = value; }
    public List<DetailItems> Result { get => result; set => result = value; }
}
