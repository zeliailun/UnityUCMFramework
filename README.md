![图片描述](UCMLogo.png)<br>

# UCM全称UnknownCreatorModules 自用框架

<框架内容有待完善>

*拥有战斗模块编辑器和管理模块编辑器<br>
*各种工具方法扩展<br>
*默认使用了小米字体<br>

---------------------------------------------------

## 需要第三方插件<br>
*Animancer<br>

---------------------------------------------------

## 模块列表<br>
*Debug模块<br>

*管理器模块<br>

*事件模块<br>

*计时器模块<br>

*场景模块<br>

*UITK模块<br>

*Json模块<br>

*分层行为状态树模块<br>

*运行时更新模块<br>

*运行时变量储存模块<br>

*对象池模块（分为引用池和Unity对象池）<br>

*修改版LitJson<br>
[<br>
*加入特性JsonIgnoreAttritube，对变量与属性跳过序列化和反序列化<br>
*加入特性JsonMarkAttritube，强制序列化和反序列化非公开变量与属性<br>
*支持反序列化多态对象时为实际类型(json序列化时，会默认对引用类型对象写入实际类型信息)<br>
*支持对输出的Json内容格式化<br>
*支持对中文字符转码<br>
*支持任意字典Key<br>
*支持string转Type运行时类型的反序列化<br>
*支持double,float正负无限大序列化和反序列化（会显示PInf,NInf字符串）<br>
*支持Unity内建类型(Vector2、Vector3、Rect、AnimationCure、Bounds、Color、Color32、Quaternion、RectOffset等）<br>
*AudioClip，Texture2D，AvatarMask等unity对象序列化时会储存其名称字符串，编辑器内通过AssetDatabase加载，运行时通过Addressables加载<br>
]<br>

*修改版序列化字典<br>
[<br>
*支持增加值类型<br>
*优化序列化效率<br>
]<br>

*修改版序列化引用<br>
[<br>
*无需Serializable属性<br>
*支持泛型<br>
]<br>

*相机模块<br>

*单位控制器模块<br>

*实体模块<br>

*声音模块<br>

*特效模块<br>

*时间模块<br>

*战斗模块（分为 单位，能力，buff，伤害，等级经验，统计，状态，天赋，投射物）<br>
