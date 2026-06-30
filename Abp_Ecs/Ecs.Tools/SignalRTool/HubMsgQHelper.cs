using System.Collections;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace Ecs.SignalRTool;

public class HubMessage
{
    public string CliMethod { get; set; }
    public object Data { get; set; }

    public HubMessage(string method, object data)
    {
        CliMethod = method;
        Data = data;
    }
}

public class HubMsgQHelper : ISingletonDependency
{
    private readonly Queue mQueue;
    private readonly object mlocker;

    public HubMsgQHelper()
    {
        mQueue = new Queue();
        mlocker = new object();
    }
    
    public void SendMessage(string hubClientMethod, object data)
    {
        lock(mlocker)
        {
            if(string.IsNullOrEmpty(hubClientMethod))
                return;

            if(mQueue.Count == 0)
            {
                mQueue.Enqueue(new HubMessage(hubClientMethod, data));
                return;
            }

            object[] items = mQueue.ToArray();  
            mQueue.Clear();

            //剔除相同类别的消息
            List<HubMessage> msgs = new List<HubMessage>();
            foreach(var item in items)
            {
                HubMessage msg = (HubMessage)item;
                if(msg.CliMethod != hubClientMethod)
                    msgs.Add(msg);
            }
            msgs.Add(new HubMessage(hubClientMethod, data));

            foreach(var m in msgs)
                mQueue.Enqueue(m);
        }        
    }

    public HubMessage GetMessage()
    {
        lock(mlocker)
        {
            if(mQueue.Count > 0)
                return (HubMessage)mQueue.Dequeue();
            return null;
        }
    }
}