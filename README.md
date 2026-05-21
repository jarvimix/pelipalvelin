# Pelipalvelin – UDP Game Server

A UDP-based two-player game server implemented in C# as a state machine.
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

## Stack

- C# / .NET 9.0
- UDP sockets
- Protocol state machine
