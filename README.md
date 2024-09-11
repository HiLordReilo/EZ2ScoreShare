# EZ2ScoreShare
Share scores between EZ2AC machines as if there is online ranking once again!

# Usage
This is a console application and is recommended to be used with some batch script running in the background.
You also need to find or host your own **PostgreSQL** database to connect to.

Console: `EZ2ScoreShare {server} {user} {password} {database} {table} {defaultName="EZ2AC_FN"}`

`databaseHostname` - IP/hostname of a PostgreSQL Server. 
`user` - Username to use for a database access. 
`password` - Password to use for a database access. 
`database` - Database that holds table with scores. 
`table` - Table that holds scores. 
`defaultName` - Optional. Sets the player name for "empty" scores. If not specified, defaults to `EZ2AC_FN` which is used in FINAL/:EX. 

> [!NOTE]
> Only tested with FINAL:EX, but considering how basic the ranking file format is (which is just 5 strings next to 5 int32s btw), I think it will work for any game with local rankings