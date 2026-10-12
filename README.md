# Pelipalvelin – UDP Game Server and Client

A UDP-based two-player game server and client implemented in C# as protocol state machines.
Built as part of a network programming course (ITKP104, grade 5).

## State Machine

![Pelipalvelimen tilakone](https://github.com/user-attachments/assets/4372c596-0bc1-4912-96b5-e27c3c0bfc76)

## How It Works

The server manages a full two-player session lifecycle over UDP:

- **CLOSED → WAIT**: Server starts, waits for players to join
- **WAIT**: First player joins (ACK 201), waits for second player
- **WAIT → GAME**: Second player joins (ACK 202 & 203), game starts
- **GAME**: Players take turns guessing a number (0–5). Correct guess sends QUIT 501/502 and ends the round
- **GAME → WAIT_ACK**: Valid move acknowledged (ACK 300), turn switches to opponent
- **WAIT_ACK → GAME**: Opponent acknowledges, game continues
- **END**: All acknowledgements received, session ends, server returns to CLOSED

Error handling: unknown messages receive ACK 407.

## Client

`peliasiakas/` is the matching client, also a state machine: **CLOSED → JOIN → GAME ⇄ TURN → CLOSED**

- **JOIN**: sends `JOIN`, waits for ACK 201/202/203
- **GAME**: waits for the opponent's guess (`DATA n`) or the result (`QUIT 501/502`, answered with `ACK 500`)
- **TURN**: after acknowledging the opponent's guess with ACK 300, sends its own guess
- On ACK 407 it resends its guess. If the server does not reply within 5 s, it stops cleanly

## Run

```bash
dotnet run --project pelipalvelin          # server on UDP port 5000
dotnet run --project peliasiakas           # player 1 (first guess 0)
dotnet run --project peliasiakas -- 2      # player 2 (first guess 2)
```

## Stack

- C# / .NET 9.0
- UDP sockets
- Protocol state machine
