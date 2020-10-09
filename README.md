# Launcher-Agent

이 프로젝트는 웹에서 클라이언트 Windows PC에 프로그램을 실행 하기 위해 도와주는 프로그램입니다.  
> 기본 포트: 49862
  
## 사용 방법
1. 변수 하나를 생성한 다음 다음 메소드중 하나를 호출합니다.  
2. WebSocket 클라이언트 생성 후 연결이 되면 json 데이터를 전송합니다.

- 프로그램 실행 방법
``` javascript
var json = RunFile('Program Path', 'Arguments');
function RunFile(path, args){
    // 리스트 생성
    var sendList = new Array() ;

    // 객체 생성
    var data = new Object() ;

    data.method = 'RunFile';
    data.path = path;
    data.args = args;
    data.version = version;

    // 리스트에 생성된 객체 삽입
    sendList.push(data) ;

    // String 형태로 변환
    var jsonData = JSON.stringify(sendList);

    console.log(jsonData);

    return jsonData;
}
```
- 원격 데스크톱 사용 방법
```javascript
var json = RunRDP('url', 'port', 'user', 'pwd');
function RunRDP(url, port, user, pwd){
    // 리스트 생성
    var sendList = new Array() ;

    // 객체 생성
    var data = new Object() ;

    data.method = 'RunRDP';
    data.url = url;
    data.port = port;
    data.user = user;
    data.pwd = pwd;
    data.version = version;

    // 리스트에 생성된 객체 삽입
    sendList.push(data) ;

    // String 형태로 변환
    var jsonData = JSON.stringify(sendList);

    console.log(jsonData);

    return jsonData;
}
```
