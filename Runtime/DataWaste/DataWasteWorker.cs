using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

internal class DataWasteWorker
{
    private readonly HttpClient httpClient;

    private readonly Thread workerThread;
    private readonly BlockingCollection<MessageWrapper> messageQueue = new BlockingCollection<MessageWrapper>();
    private readonly string playerId;
    private readonly TaskCompletionSource<object> flushTask = new TaskCompletionSource<object>();

    private class GetDataMessage : IData
    {
        private string message;
        public object Data => message;

        public GetDataMessage(string message)
        {
            this.message = message;
        }
    }

    public DataWasteWorker(Uri uri, string playerId)
    {
        this.playerId = playerId;
        httpClient = new HttpClient()
        {
            BaseAddress = uri
        };

        workerThread = new Thread(WorkerLoop);
        workerThread.Start();
    }

    public Task<string> GetServerStatus()
    {
        TaskCompletionSource<string> completionSource = new TaskCompletionSource<string>();
        messageQueue.Add(new MessageWrapper(MessageWrapper.MessageType.StatusCheck, completionSource));
        return completionSource.Task;
    }

    public void SendData(IData data)
    {
        messageQueue.Add(new MessageWrapper(MessageWrapper.MessageType.SendData, data));
    }

    public Task<string> GetNewestVersion()
    {
        TaskCompletionSource<string> completionSource = new TaskCompletionSource<string>();
        messageQueue.Add(new MessageWrapper(MessageWrapper.MessageType.GetData, new GetDataMessage("version"), completionSource));
        return completionSource.Task;
    }

    public Task FlushAndFinish()
    {
        messageQueue.CompleteAdding();
        return flushTask.Task;
    }

    private void WorkerLoop()
    {
        while (!messageQueue.IsCompleted)
        {
            try
            {
                MessageWrapper message = messageQueue.Take();
                HandleMessage(message);
            }
            catch (InvalidOperationException)
            {
                //Ignore exception caused by Take() on queue completed in the meantime
            }
        }
        flushTask.SetResult(null);
    }

    private async void HandleMessage(MessageWrapper message)
    {
        switch (message.Type)
        {
            case MessageWrapper.MessageType.SendData:
                {
                    await SendMessage(message);
                }
                break;
            case MessageWrapper.MessageType.StatusCheck:
                {
                    await SendStatusCheck(message);
                }
                break;
            case MessageWrapper.MessageType.GetData:
                {
                    await GetDataFromServer(message);
                }
                break;
            default:
                break;
        }
    }

    private async Task SendStatusCheck(MessageWrapper messageWrapper)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync("status");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                messageWrapper.Task.SetResult(content);
                return;
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            messageWrapper.Task.SetResult("");
        }
    }

    private async Task SendMessage(MessageWrapper messageWrapper)
    {
        try
        {
            HttpContent content = CreateRequestBody(messageWrapper);
            HttpResponseMessage response = await httpClient.PostAsync("telemetry", content);

            if (messageWrapper.Task == null)
            {
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                messageWrapper.Task.SetResult(responseString);
                return;
            }
        }
        catch (Exception)
        {

        }
        finally
        {
            messageWrapper.Task.SetResult("");
        }
    }

    private async Task GetDataFromServer(MessageWrapper messageWrapper)
    {
        try
        {
            HttpContent content = CreateRequestBody(messageWrapper);
            HttpResponseMessage response = await httpClient.PostAsync("getData", content);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                messageWrapper.Task.SetResult(responseString);
                return;
            }
        }
        catch (Exception)
        {

        }
        finally
        {
            messageWrapper.Task.SetResult("");
        }
    }

    private HttpContent CreateRequestBody(MessageWrapper messageWrapper)
    {
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "playerId", playerId },
            { "timestamp", messageWrapper.Timestamp },
            { "data", messageWrapper.Content.Data }
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(data));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
}
