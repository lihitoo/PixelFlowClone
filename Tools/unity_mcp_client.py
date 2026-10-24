"""Project-local client for the already-running Unity HTTP MCP bridge.

No Codex/global configuration changes. Call with a JSON request file containing
tool/arguments, or method/params for a resource inspection. Records go to the
explicit output path; never echo process command lines or credentials.
"""
import argparse,json,pathlib,requests

class Client:
    def __init__(self,url='http://127.0.0.1:8080/mcp'):
        self.url=url;self.session=requests.Session();self.serial=0
        self.headers={'Accept':'application/json, text/event-stream','Content-Type':'application/json'}
        result=self.rpc('initialize',dict(protocolVersion='2024-11-05',capabilities={},clientInfo={'name':'PixelFlowHomeReview','version':'1'}))
        self.rpc('notifications/initialized',notification=True)
    def rpc(self,method,params=None,notification=False):
        self.serial+=1;payload={'jsonrpc':'2.0','method':method}
        if not notification:payload['id']=self.serial
        if params is not None:payload['params']=params
        response=self.session.post(self.url,headers=self.headers,json=payload,timeout=55)
        response.raise_for_status()
        if response.headers.get('mcp-session-id'): self.headers['Mcp-Session-Id']=response.headers['mcp-session-id']
        if not response.content:return None
        raw=response.text
        if raw.lstrip().startswith('{'): data=json.loads(raw)
        else:
            messages=[json.loads(line[6:]) for line in raw.splitlines() if line.startswith('data: ')]
            data=next((item for item in messages if item.get('id')==self.serial),None)
            if data is None:
                if notification:return None
                raise RuntimeError('MCP stream did not contain the requested response.')
        if 'error' in data:raise RuntimeError(data['error'])
        return data.get('result')
    def call(self,name,arguments):return self.rpc('tools/call',{'name':name,'arguments':arguments})

def main():
    parser=argparse.ArgumentParser();parser.add_argument('request',type=pathlib.Path);parser.add_argument('--output',type=pathlib.Path);args=parser.parse_args()
    request=json.loads(args.request.read_text(encoding='utf-8-sig'));client=Client()
    if request.get('instance'): client.call('set_active_instance',{'instance':request['instance']})
    if 'tool' in request: result=client.call(request['tool'],request.get('arguments',{}))
    else:result=client.rpc(request['method'],request.get('params'))
    if args.output:
        args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(result,ensure_ascii=True))
if __name__=='__main__':main()
