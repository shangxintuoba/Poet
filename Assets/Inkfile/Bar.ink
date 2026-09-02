// Converted from E:\\poetobsidian\\MapNode\\Bar.md
// These functions must be bound from TextManager after each new Story is created.
EXTERNAL GetTime()
EXTERNAL GetBradPitProgress()
EXTERNAL ChangeMoney(amount)
EXTERNAL ChangeWillPower(amount)
EXTERNAL CreateCard(cardId)

-> Bar

=== Bar ===

{ GetTime() >= 1080:
    -> OpenBar
- else:
    { GetTime() < 120:
        -> OpenBar
    - else:
        你家楼下的酒吧，一家生意很差的社区店。老实说你都不知道他们为什么还没有倒闭。

        还没有开门，六点之后再来吧。
        -> END
    }
}

=== OpenBar ===

你家楼下的酒吧，一家生意很差的社区店。老实说你都不知道他们为什么还没有倒闭。

+ [买酒]
    ~ ChangeMoney(-1)
    ~ CreateCard("f22")
    你买了一杯酒。
    -> END

+ [闲聊]
    ~ ChangeWillPower(-1)
    ~ CreateCard("f26")
    -> END
