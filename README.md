# SHUtil
## 소개
SHUtil 프로젝트는 게임 내 기획 데이터와 같은 텍스트 기반 데이터를 다룰 경우를 위해 만들어진 편의성 서드파티 프로젝트입니다

XML/JSON 데이터를 주로 다루며, 압축, 암호화, I/O 기능과 같이 개발에서 일반적으로 쓰일 수 있는 편의성 기능들을 내포하고 있습니다

현재 구현되어 있는 기능은 다음과 같습니다
+ **XML/JSON 데이터 기반 Localization 데이터 관리 기능**
+ **XML/JSON 데이터 기반 테이블 데이터 매니저 기능**
+ **LZF 기반 데이터 압축 기능**
+ **AES-256-GCM 기반 데이터 암호화/복호화 기능**
+ **String, Path, XML 관련 편의 기능**
+ **스레드 안전 싱글톤 (Singleton / AutoSingleton)**

해당 프로젝트 특성 상 내부 코드 내용이 자주 바뀔 수 있으며 Unsafe 할 수 있다는 점을 미리 공유드립니다

## 지원 프레임워크
+ **.NET Standard 2.1**
+ **.NET 8.0**

## 기능 상세

### I18N (다국어 텍스트 관리)
`I18NTextMultiLanguage`를 이용해 XML/JSON 파일 단위로 지역별 텍스트 데이터를 로드하고 조회할 수 있습니다

생성자에서 키/값 컬럼명과 기본 지역 코드를 바로 넘기거나, `Init`으로 나중에 설정하는 방식 모두 지원합니다

```csharp
// 생성자로 바로 초기화
var i18n = new I18NTextMultiLanguage("key", "value", "KR");

// 로드 완료 콜백 등록
i18n.OnLoadedCallback += (filePath, regionCode, success) => { ... };

// fire-and-forget 방식 (완료는 콜백으로 감지)
i18n.LoadDataAsync("path/to/lang_KR.xml", eTableDataType.XML, "KR");

// await 가능한 방식
await i18n.LoadAsync("path/to/lang_US.xml", eTableDataType.XML, "US");

// 텍스트 조회 (지역 코드 생략 시 기본 지역 코드 자동 사용)
string text   = i18n.GetText("hello");        // 기본 지역(KR)으로 조회
string textUS = i18n.GetText("hello", "US");  // US 지역으로 조회
```

+ 지원 포맷: **XML**, **JSON**
+ 암호화 / 압축된 파일도 로드 가능
+ 동일 키 중복 추가 시 첫 번째 값 유지
+ `OnLoadedCallback` 이벤트로 로드 완료 감지

---

### 테이블 데이터 관리
`TableOwnerBase<T>`를 상속해 XML/JSON 파일 기반 테이블 데이터를 로드하고 키로 조회할 수 있습니다

```csharp
// 1. 데이터 클래스 정의
public class MonsterInfo : TableInfoXmlBase
{
    public int    Id;
    public string Name;

    protected override void Load(XmlNode node)
    {
        Id   = XmlUtil.ParseAttribute<int>(node, "id", 0);
        Name = XmlUtil.ParseAttribute<string>(node, "name", "");
    }

    protected override void LoadAppend(XmlNode node) { }
}

// 2. 테이블 클래스 정의
public class MonsterTable : TableOwnerBase<MonsterInfo>
{
    public override string             FileName         => "MonsterTable";
    public override bool               IsStringKey      => false;
    public override bool               CheckSameKey     => true;
    public override eTableDataType     TableDataType    => eTableDataType.XML;
    public override ITableDataTypeInfo TableDataTypeInfo => TableDataTypeInfoUtil.DefaultXMLInfo;
}

// 3. 로드 및 조회
var table = new MonsterTable();
await table.LoadAsync("path/to/MonsterTable.xml");  // await 가능
// or: table.LoadData("path/to/MonsterTable.xml");  // 동기

var monster = table.GetInfoByIntKey(1001);
var first   = table.GetInfoByIndex(0);

// DataListManager로 통합 관리
DataListManager.Instance.Init("data/");
DataListManager.Instance.Load(table, "xml");

var monster = DataListManager.Instance.Get<MonsterTable>()?.GetInfoByIntKey(1001);
```

+ 지원 포맷: **XML**, **JSON**
+ 키 타입: **int** (`IsStringKey = false`) / **string** (`IsStringKey = true`)
+ `CheckSameKey = false` 설정 시 중복 키 → `LoadAppend` 호출로 append 방식 처리
+ `LoadAsync` (await 가능) / `LoadDataAsync` (fire-and-forget) 모두 지원
+ 암호화 / 압축된 파일도 로드 가능

---

### 데이터 압축 (LZF)
LZF 알고리즘 기반 데이터 압축/해제 기능입니다

```csharp
byte[] compressed   = CLZF.Compress(rawBytes);
byte[] decompressed = CLZF.Decompress(compressed);
```

---

### 암호화/복호화 (AES-256-GCM)
AES-256-GCM 방식으로 파일 또는 바이트 데이터를 암호화하고 복호화합니다

```csharp
// 파일 암호화/복호화
FileUtil.Encrypt("source.xml", "output.enc", "password");
byte[] plain = FileUtil.Decrypt("output.enc", "password");

// 바이트 단위 복호화
byte[] plainBytes = FileUtil.DecryptWithBytes(encryptedBytes, "password");
```

---

### 싱글톤 (Singleton / AutoSingleton)
+ `Singleton<T>`: 스레드 안전 정적 싱글톤 (readonly static 필드 기반)
+ `AutoSingleton<T>`: 프로세스 종료 시 `DisposeSingleton`이 자동 호출되는 싱글톤

```csharp
// Singleton
var instance = MySingleton.Instance;

// AutoSingleton (DataListManager 등이 이 방식을 사용)
DataListManager.Instance.Init("data/");
DataListManager.Instance.DisposeSingleton(); // 수동 해제도 가능
```

---

### 유틸리티
+ **StringUtil**: 타입 파싱, 리스트 파싱, 문자열 압축/분할
+ **PathUtil**: 파일 경로/URL 유효성 검사, URL 존재 여부 비동기 확인
+ **XmlUtil**: XML 노드/속성 파싱, 직렬화/역직렬화, 저장/불러오기
+ **XmlSelector**: XmlNode / XmlReader / XmlBinary를 통합 접근하는 래퍼

## 서드파티 사용처
+ [ButterBot(개인 프로젝트)](https://github.com/Shoorito/butter_bot)
+ [TBL_Exporter(개인 프로젝트)](https://github.com/Shoorito/TBL_Exporter)
