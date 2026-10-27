import 'package:flutter_test/flutter_test.dart';
import 'package:view621/helpers/dpapi_helper.dart';

void main() {
  test('DPAPI encryption and decryption', () {
    const secret = 'super_secret_api_key_12345';
    final encrypted = DpapiHelper.encrypt(secret);
    expect(encrypted, isNotNull);
    expect(encrypted, isNot(equals(secret)));

    final decrypted = DpapiHelper.decrypt(encrypted);
    expect(decrypted, equals(secret));
  });
}
