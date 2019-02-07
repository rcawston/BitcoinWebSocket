# BitcoinWebSocket
Uses ZMQ interface to monitor bitcoin transactions and blocks, decodes raw data internally, and publishes subscribed transactions via websocket.

Can watch (subscribe to) addresses (including bech32 native segwit), or OP_RETURN data prefixes. Could be trivially extended to monitor any subset of transactions (see BlockConsumer and TXConsumer).

## External Requirements
- Bitcoin Node w/ ZMQ Enabled (zmqpubrawblock=\<address> and zmqpubrawtx=\<address> config options)
