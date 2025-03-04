**一个C#分析器及源生成器**

#### 已完成的功能
- `string==null`和`string==""`禁用，强制使用`string.IsNullOrEmpty`(警告)
- `string!=null`和`string!=""`禁用，强制使用`!string.IsNullOrEmpty`(警告)
- 调用方法时，可选参数必须显示命名(警告)(一键修复)
- 方法返回值禁止为`async void`(错误)(一键修复)
- lambda表达式返回值禁止为`async void`(错误)
- async方法，方法名强制以`Async`为后缀(警告)(一键修复)
- 返回值为`System.Threading.Tasks.Task`和`System.Threading.Tasks.Task<T>`的方法，方法名强制以`Async`为后缀(警告)(一键修复)
- `PickFieldsAttribute`可以从其他类型挑选字段(源生成器)
- 元组类型定义时，成员必须显式命名
- switch语句增加throw默认值(一键修复)

#### 后续计划
- `ToJsonStringAttribute(bool isOverride)`为类型重写`ToString`方法，将`this`序列化为Json字符串
  - `isOverride`为`true`时，重写`ToString`方法
  - `isOverride`为`false`时，增加一个`ToJsonString`方法
- switch语句的throw默认值只有在switch语句没有默认值时显示
