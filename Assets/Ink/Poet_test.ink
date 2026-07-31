// Branch completion state
VAR barD1_1Finished = false
VAR barD1_2Finished = false
VAR barD1_3Finished = false

// Converted from Poet_test.twee

// Leading asterisk notes keep their original text. An invisible prefix prevents Ink reading them as choices.

-> Bar_D1

=== Bar_D1 ===

“刚刚说到哪了？”老板问你。

+ [我的工作] -> Bar_D1_1
+ [你的工作] -> Bar_D1_2
+ [腐蚀腺] -> Bar_D1_3

=== Bar_D1_1 ===

“想的怎么样了？你已经盯着本子在这儿坐了一晚上了。”


+ [写不出来] -> Bar_D1_11

=== Bar_D1_11 ===

“自从你来之后我就没见你有写的出来的时候，唉。上周那篇你后来交了什么上去？”


+ [一个悬疑故事] -> Bar_D1_111
+ [一个恐怖故事，讲一座会吃人的岛] -> Bar_D1_112
+ [一个关于机器人的科幻故事] -> Bar_D1_113

=== Bar_D1_111 ===

​*谜+1，城市+1*
凶手是谁？

+ [一个精神病人] -> Bar_D1_1111
+ [一把椅子] -> Bar_D1_1112
+ [一个退伍老兵] -> Bar_D1_1113
+ [没有人死] -> Bar_D1_1114

=== Bar_D1_1111 ===

​*疯狂+1
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_1112 ===

​*反讽+1
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_1113 ===

​*战争+1
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_1114 ===

​*沉默+1*
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_112 ===

​*机器+1，海+1*

”然后呢？“

+ [一个机器人爱上了一个游泳运动员，于是试图去学游泳，最后短路而死] -> Bar_D1_1121
+ [一搜潜水艇获得了意识，试图像鱼一样生活] -> Bar_D1_1122
+ [一个泳池清洁机器人被改造成了艺术家，一直在画它看到的泳池瓷砖] -> Bar_D1_1123

=== Bar_D1_1121 ===

​*牺牲+1*
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_1122 ===

​*野兽+1*
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_1123 ===

​*幻觉+1*
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_113 ===

​*恐惧+1，岛+1*

”然后呢？“

+ [岛是活的] -> Bar_D1_1131
+ [人们自相残杀] -> Bar_D1_1132
+ [根本就没有这座岛] -> Bar_D1_1133

=== Bar_D1_1131 ===

​*肉身+1*
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_1132 ===

​*罪+1*
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_1133 ===

​*幻觉+1*
“不错的故事。”老板说

~ barD1_1Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_2 ===

“我的工作？没什么好聊的。做酒，跟客人聊天，清理他们的呕吐物，下班后去路口吃夜宵。”

+ [有什么好玩的客人么] -> Bar_D1_21
+ [有什么特别恶劣的客人么] -> Bar_D1_21

=== Bar_D1_21 ===

“原则上来说我们是要保护客人的隐私的。”

+ [一点都不能说么] -> Bar_D1_211
~ barD1_2Finished = true
+ [好吧] -> Bar_D1_21B

=== Bar_D1_211 ===

“不行。”老板笑了笑。
“但你可以试着再点一杯酒，说不定再醉一点就会有更多灵感了”

~ barD1_2Finished = true
+ [好吧] -> Bar_D1_21B
+ [可我现在*真的*写不出来了] -> Bar_D1_2112
+ [你就是想多卖一杯酒] -> Bar_D1_2113

=== Bar_D1_2112 ===

“不行哦”老板摇了摇头。
“但既然你这么说了，那我送你一杯酒吧”

老板给你倒了一个shot

~ barD1_2Finished = true
+ [算了] -> Bar_D1_21B
+ [拿过杯子] -> Bar_D1_21122

=== Bar_D1_21122 ===

​*意志力回满

~ barD1_2Finished = true
+ [换个话题] -> Bar_D1_21B

=== Bar_D1_2113 ===

"我就那么坏么"老板笑了。

“但既然你这么说了，那我送你一杯酒吧”

老板给你倒了一个shot

~ barD1_2Finished = true
+ [算了] -> Bar_D1_21B
+ [拿过杯子] -> Bar_D1_21122

=== Bar_D1_21B ===

聊点什么呢？

+ {barD1_1Finished == false} [我的工作] -> Bar_D1_1
+ {barD1_2Finished == false} [你的工作] -> Bar_D1_2
+ {barD1_3Finished == false} [腐蚀腺] -> Bar_D1_3

=== Bar_D1_3 ===

“你不会真的相信刚才那个神棍的鬼话吧？”老板有些诧异。
“腐蚀腺，受体，肛门，盐，听起来简直像是中世纪炼金术”

+ [我对神秘学一直很感兴趣] -> Bar_D1_31
+ [老实说我太懂，但他看起来还挺有意思的] -> Bar_D1_31
+ [一点也不信，我就是想找些素材] -> Bar_D1_31

=== Bar_D1_31 ===

“哎，明天再来吧，他每天半夜都会来这里的”

~ barD1_3Finished = true
+ [换个话题] -> Bar_D1_21B
