// Converted from E:\\poetobsidian\\MapNode\\Home_Street.md
// GetTime returns the current time as minutes since 00:00.
EXTERNAL GetTime()
EXTERNAL CreateCard(cardId)

-> Home_Street

=== Home_Street ===

{ GetTime() >= 360 && GetTime() < 780:
    你家楼下的街道。
    -> END
- else:
    { GetTime() >= 780 && GetTime() < 960:
        你家楼下的街道。
        太阳晒得你有些犯困。
        -> END
    - else:
        { GetTime() >= 960 && GetTime() < 1140:
            你家楼下的街道。
            台风天之后的云总是很好看。

            + [看一会儿日落]
                ~ CreateCard("f25")
                你看着太阳缓缓落下。
                -> END
        - else:
            { GetTime() >= 1140:
                你家楼下的街道。
                -> END
            - else:
                { GetTime() < 120:
                    你家楼下的街道。
                    -> END
                - else:
                    你家楼下的街道。
                    夜很深了。
                    -> END
                }
            }
        }
    }
}
