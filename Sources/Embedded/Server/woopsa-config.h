#ifndef __WOOPSA_CONFIG_H_
#define __WOOPSA_CONFIG_H_

// Woopsa uses these internally, allowing you to use Woopsa in a
// thread-safe manner, or disabling interrupts. Just fill this in
// if needed with whatever locking mechanism your environment has.
#define WOOPSA_LOCK
#define WOOPSA_UNLOCK

#endif